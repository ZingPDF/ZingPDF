using System.Security.Cryptography;
using System.Text;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.Encryption;

internal enum StandardCryptMethod
{
    Identity = 0,
    Rc4 = 1,
    Aes128 = 2,
    Aes256 = 3,
}

internal sealed record StandardSecurityHandlerOptions(
    int Version,
    int Revision,
    int KeyLengthBits,
    int Permissions,
    byte[] OwnerValue,
    byte[] UserValue,
    bool EncryptMetadata,
    byte[] FileId,
    StandardCryptMethod StringMethod,
    StandardCryptMethod StreamMethod,
    byte[]? OwnerEncryptionKey = null,
    byte[]? UserEncryptionKey = null,
    byte[]? EncryptedPermissions = null,
    IndirectObjectId? EncryptionDictionaryId = null
);

internal sealed class StandardSecurityHandler : ISecurityHandler
{
    private static readonly byte[] PasswordPadding =
    [
        0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41,
        0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
        0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80,
        0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A
    ];

    private readonly StandardSecurityHandlerOptions _options;
    private readonly int _keyLengthBytes;
    private byte[]? _fileKey;

    public StandardSecurityHandler(StandardSecurityHandlerOptions options)
    {
        _options = options;
        _keyLengthBytes = options.Version >= 5
            ? 32
            : Math.Clamp(options.KeyLengthBits / 8, 5, 16);
    }

    public static StandardSecurityHandlerOptions CreateNewOptions(
        string userPassword,
        string ownerPassword,
        byte[] fileId,
        int permissions,
        PdfEncryptionAlgorithm algorithm,
        bool encryptMetadata,
        IndirectObjectId? encryptionDictionaryId = null)
    {
        ArgumentNullException.ThrowIfNull(fileId);

        return algorithm switch
        {
            PdfEncryptionAlgorithm.Rc4_128 => CreateRc4Options(userPassword, ownerPassword, fileId, permissions, encryptMetadata, encryptionDictionaryId),
            PdfEncryptionAlgorithm.Aes128 => CreateAes128Options(userPassword, ownerPassword, fileId, permissions, encryptMetadata, encryptionDictionaryId),
            PdfEncryptionAlgorithm.Aes256 => CreateAes256Options(userPassword, ownerPassword, fileId, permissions, encryptMetadata, encryptionDictionaryId),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
        };
    }

    public bool IsAuthenticated => _fileKey is not null;

    public bool TryAuthenticate(string password)
    {
        if (_options.Version >= 5)
        {
            return TryAuthenticateV5(password);
        }

        return TryAuthenticateLegacy(password);
    }

    public bool ShouldDecrypt(IndirectObjectId objectId, IStreamDictionary? streamDictionary)
    {
        if (_options.EncryptionDictionaryId is not null && _options.EncryptionDictionaryId == objectId)
        {
            return false;
        }

        if (!ShouldEncryptMetadata(streamDictionary))
        {
            return false;
        }

        if (streamDictionary is CrossReferenceStreamDictionary)
        {
            return false;
        }

        return GetCryptMethod(streamDictionary) != StandardCryptMethod.Identity;
    }

    public byte[] Encrypt(IndirectObjectId objectId, byte[] data)
        => Transform(objectId, data, null, encrypt: true);

    public byte[] Decrypt(IndirectObjectId objectId, byte[] data)
        => Transform(objectId, data, null, encrypt: false);

    public byte[] Transform(IndirectObjectId objectId, byte[] data, IStreamDictionary? streamDictionary, bool encrypt)
    {
        if (_fileKey is null)
        {
            throw new InvalidOperationException("Cannot transform encrypted data without a file encryption key.");
        }

        var method = GetCryptMethod(streamDictionary);
        return method switch
        {
            StandardCryptMethod.Identity => data,
            StandardCryptMethod.Rc4 => Rc4(DeriveObjectKey(objectId, _fileKey, useAesSalt: false, version: _options.Version), data),
            StandardCryptMethod.Aes128 => TransformAesCbc(DeriveObjectKey(objectId, _fileKey, useAesSalt: true, version: _options.Version), data, encrypt),
            StandardCryptMethod.Aes256 => TransformAesCbc(_fileKey, data, encrypt),
            _ => throw new NotSupportedException($"Unsupported crypt method: {method}."),
        };
    }

    private bool TryAuthenticateLegacy(string password)
    {
        var padded = PadPassword(password);

        var fileKey = ComputeLegacyFileKey(padded);
        if (ValidateLegacyUserPassword(fileKey))
        {
            _fileKey = fileKey;
            return true;
        }

        var ownerPassword = TryGetOwnerDerivedUserPassword(password);
        if (ownerPassword is null)
        {
            return false;
        }

        var ownerFileKey = ComputeLegacyFileKey(ownerPassword);
        if (!ValidateLegacyUserPassword(ownerFileKey))
        {
            return false;
        }

        _fileKey = ownerFileKey;
        return true;
    }

    private bool TryAuthenticateV5(string password)
    {
        var passwordBytes = NormalizeV5Password(password);
        byte[]? keySalt = null;
        byte[] userData = [];
        byte[]? encryptedFileKey = null;

        if (CheckOwnerPasswordV5(passwordBytes))
        {
            keySalt = _options.OwnerValue.AsSpan(40, 8).ToArray();
            userData = _options.UserValue.AsSpan(0, 48).ToArray();
            encryptedFileKey = _options.OwnerEncryptionKey;
        }
        else if (CheckUserPasswordV5(passwordBytes))
        {
            keySalt = _options.UserValue.AsSpan(40, 8).ToArray();
            encryptedFileKey = _options.UserEncryptionKey;
        }

        if (keySalt is null || encryptedFileKey is null)
        {
            return false;
        }

        var intermediateKey = HashV5(passwordBytes, keySalt, userData, _options.Revision);
        var fileKey = AesCbcTransform(intermediateKey, encryptedFileKey, encrypt: false, iv: null, usePadding: false);

        if (_options.EncryptedPermissions is not null)
        {
            var decryptedPerms = AesCbcTransform(fileKey, _options.EncryptedPermissions, encrypt: false, iv: null, usePadding: false);
            var expectedPerms = ComputePermissionsBlock(_options.Permissions, _options.EncryptMetadata, fillRandomTail: false);
            if (!decryptedPerms.AsSpan(0, 12).SequenceEqual(expectedPerms.AsSpan(0, 12)))
            {
                return false;
            }
        }

        _fileKey = fileKey;
        return true;
    }

    private byte[] ComputeLegacyFileKey(byte[] paddedPassword)
        => ComputeLegacyFileKey(
            paddedPassword,
            _options.OwnerValue,
            _options.Permissions,
            _options.FileId,
            _options.Revision,
            _options.KeyLengthBits,
            _options.EncryptMetadata);

    private bool ValidateLegacyUserPassword(byte[] fileKey)
    {
        using var md5 = MD5.Create();

        if (_options.Revision == 2)
        {
            var result = Rc4(fileKey, PasswordPadding);
            return result.AsSpan().SequenceEqual(_options.UserValue);
        }

        var data = new byte[PasswordPadding.Length + _options.FileId.Length];
        PasswordPadding.CopyTo(data, 0);
        _options.FileId.CopyTo(data, PasswordPadding.Length);

        var hash = md5.ComputeHash(data);
        var encrypted = Rc4(fileKey, hash);
        for (var i = 1; i <= 19; i++)
        {
            encrypted = Rc4(XorKey(fileKey, i), encrypted);
        }

        return _options.UserValue.AsSpan(0, 16).SequenceEqual(encrypted.AsSpan(0, 16));
    }

    private bool CheckUserPasswordV5(byte[] password)
        => HashV5(password, _options.UserValue.AsSpan(32, 8).ToArray(), [], _options.Revision)
            .SequenceEqual(_options.UserValue.AsSpan(0, 32).ToArray());

    private bool CheckOwnerPasswordV5(byte[] password)
        => HashV5(password, _options.OwnerValue.AsSpan(32, 8).ToArray(), _options.UserValue.AsSpan(0, 48).ToArray(), _options.Revision)
            .SequenceEqual(_options.OwnerValue.AsSpan(0, 32).ToArray());

    private byte[]? TryGetOwnerDerivedUserPassword(string ownerPassword)
    {
        if (_options.Revision < 2)
        {
            return null;
        }

        using var md5 = MD5.Create();

        var paddedOwner = PadPassword(ownerPassword);
        var hash = md5.ComputeHash(paddedOwner);

        if (_options.Revision >= 3)
        {
            for (var i = 0; i < 50; i++)
            {
                hash = md5.ComputeHash(hash);
            }
        }

        var key = hash.AsSpan(0, _keyLengthBytes).ToArray();
        if (_options.Revision == 2)
        {
            return Rc4(key, _options.OwnerValue);
        }

        var result = _options.OwnerValue.ToArray();
        for (var i = 19; i >= 0; i--)
        {
            result = Rc4(XorKey(key, i), result);
        }

        return result;
    }

    private StandardCryptMethod GetCryptMethod(IStreamDictionary? streamDictionary)
        => streamDictionary is null ? _options.StringMethod : _options.StreamMethod;

    private bool ShouldEncryptMetadata(IStreamDictionary? streamDictionary)
    {
        if (_options.EncryptMetadata)
        {
            return true;
        }

        return streamDictionary?.Type != Constants.DictionaryTypes.Metadata;
    }

    private static StandardSecurityHandlerOptions CreateRc4Options(
        string userPassword,
        string ownerPassword,
        byte[] fileId,
        int permissions,
        bool encryptMetadata,
        IndirectObjectId? encryptionDictionaryId)
    {
        const int version = 2;
        const int revision = 3;
        const int keyLengthBits = 128;

        var sanitizedOwnerPassword = string.IsNullOrEmpty(ownerPassword) ? userPassword : ownerPassword;
        var ownerValue = ComputeOwnerValue(userPassword, sanitizedOwnerPassword, revision, keyLengthBits);
        var fileKey = ComputeLegacyFileKey(PadPassword(userPassword), ownerValue, permissions, fileId, revision, keyLengthBits, encryptMetadata);
        var userValue = ComputeLegacyUserValue(fileKey, fileId, revision);

        return new StandardSecurityHandlerOptions(
            version,
            revision,
            keyLengthBits,
            permissions,
            ownerValue,
            userValue,
            encryptMetadata,
            fileId,
            StandardCryptMethod.Rc4,
            StandardCryptMethod.Rc4,
            EncryptionDictionaryId: encryptionDictionaryId);
    }

    private static StandardSecurityHandlerOptions CreateAes128Options(
        string userPassword,
        string ownerPassword,
        byte[] fileId,
        int permissions,
        bool encryptMetadata,
        IndirectObjectId? encryptionDictionaryId)
    {
        const int version = 4;
        const int revision = 4;
        const int keyLengthBits = 128;

        var sanitizedOwnerPassword = string.IsNullOrEmpty(ownerPassword) ? userPassword : ownerPassword;
        var ownerValue = ComputeOwnerValue(userPassword, sanitizedOwnerPassword, revision, keyLengthBits);
        var fileKey = ComputeLegacyFileKey(PadPassword(userPassword), ownerValue, permissions, fileId, revision, keyLengthBits, encryptMetadata);
        var userValue = ComputeLegacyUserValue(fileKey, fileId, revision);

        return new StandardSecurityHandlerOptions(
            version,
            revision,
            keyLengthBits,
            permissions,
            ownerValue,
            userValue,
            encryptMetadata,
            fileId,
            StandardCryptMethod.Aes128,
            StandardCryptMethod.Aes128,
            EncryptionDictionaryId: encryptionDictionaryId);
    }

    private static StandardSecurityHandlerOptions CreateAes256Options(
        string userPassword,
        string ownerPassword,
        byte[] fileId,
        int permissions,
        bool encryptMetadata,
        IndirectObjectId? encryptionDictionaryId)
    {
        const int version = 5;
        const int revision = 6;
        const int keyLengthBits = 256;

        var fileKey = RandomNumberGenerator.GetBytes(32);

        var userPasswordBytes = NormalizeV5Password(userPassword);
        var userValidationSalt = RandomNumberGenerator.GetBytes(8);
        var userKeySalt = RandomNumberGenerator.GetBytes(8);
        var userHash = HashV5(userPasswordBytes, userValidationSalt, [], revision);
        var userValue = userHash.Concat(userValidationSalt).Concat(userKeySalt).ToArray();
        var userEncryptionKey = AesCbcTransform(HashV5(userPasswordBytes, userKeySalt, [], revision), fileKey, encrypt: true, iv: null, usePadding: false);

        var ownerPasswordBytes = NormalizeV5Password(string.IsNullOrEmpty(ownerPassword) ? userPassword : ownerPassword);
        var ownerValidationSalt = RandomNumberGenerator.GetBytes(8);
        var ownerKeySalt = RandomNumberGenerator.GetBytes(8);
        var ownerHash = HashV5(ownerPasswordBytes, ownerValidationSalt, userValue, revision);
        var ownerValue = ownerHash.Concat(ownerValidationSalt).Concat(ownerKeySalt).ToArray();
        var ownerEncryptionKey = AesCbcTransform(HashV5(ownerPasswordBytes, ownerKeySalt, userValue, revision), fileKey, encrypt: true, iv: null, usePadding: false);

        var permissionsBlock = ComputePermissionsBlock(permissions, encryptMetadata, fillRandomTail: true);
        var encryptedPermissions = AesCbcTransform(fileKey, permissionsBlock, encrypt: true, iv: null, usePadding: false);

        return new StandardSecurityHandlerOptions(
            version,
            revision,
            keyLengthBits,
            permissions,
            ownerValue,
            userValue,
            encryptMetadata,
            fileId,
            StandardCryptMethod.Aes256,
            StandardCryptMethod.Aes256,
            ownerEncryptionKey,
            userEncryptionKey,
            encryptedPermissions,
            encryptionDictionaryId);
    }

    private static byte[] PadPassword(string? password)
    {
        var bytes = string.IsNullOrEmpty(password)
            ? []
            : Encoding.ASCII.GetBytes(password);

        var padded = new byte[32];
        var count = Math.Min(bytes.Length, padded.Length);
        Array.Copy(bytes, padded, count);
        if (count < padded.Length)
        {
            Array.Copy(PasswordPadding, 0, padded, count, padded.Length - count);
        }

        return padded;
    }

    private static byte[] NormalizeV5Password(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return [];
        }

        var bytes = Encoding.UTF8.GetBytes(password);
        return bytes.Length <= 127 ? bytes : bytes.AsSpan(0, 127).ToArray();
    }

    private static byte[] DeriveObjectKey(IndirectObjectId objectId, byte[] fileKey, bool useAesSalt, int version)
    {
        if (version >= 5)
        {
            return fileKey;
        }

        using var md5 = MD5.Create();
        var buffer = new List<byte>(fileKey.Length + 9);
        buffer.AddRange(fileKey);

        var index = objectId.Index;
        buffer.Add((byte)(index & 0xFF));
        buffer.Add((byte)((index >> 8) & 0xFF));
        buffer.Add((byte)((index >> 16) & 0xFF));

        var generation = objectId.GenerationNumber;
        buffer.Add((byte)(generation & 0xFF));
        buffer.Add((byte)((generation >> 8) & 0xFF));

        if (useAesSalt)
        {
            buffer.AddRange("sAlT"u8.ToArray());
        }

        var hash = md5.ComputeHash(buffer.ToArray());
        var keyLength = Math.Min(16, fileKey.Length + 5);
        return hash.AsSpan(0, keyLength).ToArray();
    }

    private static byte[] TransformAesCbc(byte[] key, byte[] data, bool encrypt)
    {
        if (encrypt)
        {
            var initializationVector = RandomNumberGenerator.GetBytes(16);
            var encrypted = AesCbcTransform(key, data, encrypt: true, initializationVector, usePadding: true);
            return initializationVector.Concat(encrypted).ToArray();
        }

        if (data.Length < 16)
        {
            throw new InvalidPdfException("AES-encrypted object data is missing its initialization vector.");
        }

        var objectIv = data.AsSpan(0, 16).ToArray();
        var payload = data.AsSpan(16).ToArray();
        return AesCbcTransform(key, payload, encrypt: false, objectIv, usePadding: true);
    }

    private static byte[] AesCbcTransform(byte[] key, byte[] data, bool encrypt, byte[]? iv, bool usePadding)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv ?? new byte[16];
        aes.Mode = CipherMode.CBC;
        aes.Padding = usePadding ? PaddingMode.PKCS7 : PaddingMode.None;

        using var transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor();
        return transform.TransformFinalBlock(data, 0, data.Length);
    }

    private static byte[] Rc4(byte[] key, byte[] data)
    {
        var s = new byte[256];
        for (var i = 0; i < s.Length; i++)
        {
            s[i] = (byte)i;
        }

        var j = 0;
        for (var i = 0; i < s.Length; i++)
        {
            j = (j + s[i] + key[i % key.Length]) & 0xFF;
            (s[i], s[j]) = (s[j], s[i]);
        }

        var output = new byte[data.Length];
        var iIndex = 0;
        j = 0;
        for (var k = 0; k < data.Length; k++)
        {
            iIndex = (iIndex + 1) & 0xFF;
            j = (j + s[iIndex]) & 0xFF;
            (s[iIndex], s[j]) = (s[j], s[iIndex]);
            var t = (s[iIndex] + s[j]) & 0xFF;
            output[k] = (byte)(data[k] ^ s[t]);
        }

        return output;
    }

    private static byte[] XorKey(byte[] key, int value)
    {
        var output = new byte[key.Length];
        for (var i = 0; i < key.Length; i++)
        {
            output[i] = (byte)(key[i] ^ value);
        }

        return output;
    }

    private static byte[] ComputeOwnerValue(string userPassword, string ownerPassword, int revision, int keyLengthBits)
    {
        using var md5 = MD5.Create();

        var keyLengthBytes = Math.Clamp(keyLengthBits / 8, 5, 16);
        var paddedOwner = PadPassword(ownerPassword);
        var hash = md5.ComputeHash(paddedOwner);

        if (revision >= 3)
        {
            for (var i = 0; i < 50; i++)
            {
                hash = md5.ComputeHash(hash);
            }
        }

        var key = hash.AsSpan(0, keyLengthBytes).ToArray();
        var encrypted = Rc4(key, PadPassword(userPassword));
        if (revision >= 3)
        {
            for (var i = 1; i <= 19; i++)
            {
                encrypted = Rc4(XorKey(key, i), encrypted);
            }
        }

        return encrypted;
    }

    private static byte[] ComputeLegacyFileKey(
        byte[] paddedPassword,
        byte[] ownerValue,
        int permissions,
        byte[] fileId,
        int revision,
        int keyLengthBits,
        bool encryptMetadata)
    {
        using var md5 = MD5.Create();

        var keyLengthBytes = Math.Clamp(keyLengthBits / 8, 5, 16);
        var data = new List<byte>(paddedPassword.Length + ownerValue.Length + 16);
        data.AddRange(paddedPassword);
        data.AddRange(ownerValue);
        data.AddRange(BitConverter.GetBytes(permissions));
        data.AddRange(fileId);

        if (revision >= 4 && !encryptMetadata)
        {
            data.AddRange([0xFF, 0xFF, 0xFF, 0xFF]);
        }

        var hash = md5.ComputeHash(data.ToArray());

        if (revision >= 3)
        {
            for (var i = 0; i < 50; i++)
            {
                hash = md5.ComputeHash(hash.AsSpan(0, keyLengthBytes).ToArray());
            }
        }

        return hash.AsSpan(0, keyLengthBytes).ToArray();
    }

    private static byte[] ComputeLegacyUserValue(byte[] fileKey, byte[] fileId, int revision)
    {
        using var md5 = MD5.Create();

        if (revision == 2)
        {
            return Rc4(fileKey, PasswordPadding);
        }

        var data = new byte[PasswordPadding.Length + fileId.Length];
        PasswordPadding.CopyTo(data, 0);
        fileId.CopyTo(data, PasswordPadding.Length);

        var hash = md5.ComputeHash(data);
        var encrypted = Rc4(fileKey, hash);
        for (var i = 1; i <= 19; i++)
        {
            encrypted = Rc4(XorKey(fileKey, i), encrypted);
        }

        var userKey = new byte[32];
        encrypted.CopyTo(userKey, 0);
        Array.Copy(PasswordPadding, 0, userKey, 16, 16);
        return userKey;
    }

    private static byte[] HashV5(byte[] password, byte[] salt, byte[] userData, int revision)
    {
        using var sha256 = SHA256.Create();
        var seed = Combine(password, salt, userData);
        var k = sha256.ComputeHash(seed);

        if (revision < 6)
        {
            return k;
        }

        var roundNumber = 0;
        byte[] e = [];
        do
        {
            roundNumber++;
            var k1 = Combine(password, k, userData);
            var repeated = new byte[k1.Length * 64];
            for (var i = 0; i < 64; i++)
            {
                Buffer.BlockCopy(k1, 0, repeated, i * k1.Length, k1.Length);
            }

            e = AesCbcTransform(k.AsSpan(0, 16).ToArray(), repeated, encrypt: true, k.AsSpan(16, 16).ToArray(), usePadding: false);
            var eMod3 = 0;
            for (var i = 0; i < 16; i++)
            {
                eMod3 += e[i];
            }

            k = (eMod3 % 3) switch
            {
                0 => SHA256.HashData(e),
                1 => SHA384.HashData(e),
                _ => SHA512.HashData(e),
            };
        }
        while (roundNumber < 64 || e[^1] > roundNumber - 32);

        return k.AsSpan(0, 32).ToArray();
    }

    private static byte[] ComputePermissionsBlock(int permissions, bool encryptMetadata, bool fillRandomTail)
    {
        var block = new byte[16];
        var p = permissions;
        for (var i = 0; i < 4; i++)
        {
            block[i] = (byte)(p & 0xFF);
            p >>= 8;
        }

        block[4] = 0xFF;
        block[5] = 0xFF;
        block[6] = 0xFF;
        block[7] = 0xFF;
        block[8] = encryptMetadata ? (byte)'T' : (byte)'F';
        block[9] = (byte)'a';
        block[10] = (byte)'d';
        block[11] = (byte)'b';

        if (fillRandomTail)
        {
            RandomNumberGenerator.Fill(block.AsSpan(12, 4));
        }

        return block;
    }

    private static byte[] Combine(params byte[][] arrays)
    {
        var length = arrays.Sum(x => x.Length);
        var output = new byte[length];
        var offset = 0;
        foreach (var array in arrays)
        {
            Buffer.BlockCopy(array, 0, output, offset, array.Length);
            offset += array.Length;
        }

        return output;
    }
}
