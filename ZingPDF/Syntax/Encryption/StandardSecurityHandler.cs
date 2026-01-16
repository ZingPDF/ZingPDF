using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using ZingPDF;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.Encryption;

internal sealed record StandardSecurityHandlerOptions(
    int Version,
    int Revision,
    int KeyLengthBits,
    int Permissions,
    byte[] OwnerKey,
    byte[] UserKey,
    bool EncryptMetadata,
    byte[] FileId,
    IndirectObjectId? EncryptionDictionaryId
);

internal sealed class StandardSecurityHandler : ISecurityHandler
{
    private static readonly byte[] _passwordPadding =
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
        _keyLengthBytes = Math.Clamp(options.KeyLengthBits / 8, 5, 16);
    }

    public bool IsAuthenticated => _fileKey is not null;

    public bool TryAuthenticate(string password)
    {
        var padded = PadPassword(password);

        var fileKey = ComputeFileKey(padded);
        if (ValidateUserPassword(padded, fileKey))
        {
            _fileKey = fileKey;
            return true;
        }

        var ownerPassword = TryGetOwnerDerivedUserPassword(password);
        if (ownerPassword is null)
        {
            return false;
        }

        var ownerFileKey = ComputeFileKey(ownerPassword);
        if (!ValidateUserPassword(ownerPassword, ownerFileKey))
        {
            return false;
        }

        _fileKey = ownerFileKey;
        return true;
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

        return true;
    }

    public byte[] Encrypt(IndirectObjectId objectId, byte[] data)
        => Decrypt(objectId, data);

    public byte[] Decrypt(IndirectObjectId objectId, byte[] data)
    {
        if (_fileKey is null)
        {
            throw new InvalidOperationException("Cannot decrypt without a file encryption key.");
        }

        var objectKey = DeriveObjectKey(objectId, _fileKey);
        return Rc4(objectKey, data);
    }

    private byte[] ComputeFileKey(byte[] paddedPassword)
    {
        using var md5 = MD5.Create();

        var data = new List<byte>(paddedPassword.Length + _options.OwnerKey.Length + 16);
        data.AddRange(paddedPassword);
        data.AddRange(_options.OwnerKey);
        data.AddRange(BitConverter.GetBytes(_options.Permissions));
        data.AddRange(_options.FileId);

        if (_options.Revision >= 4 && !_options.EncryptMetadata)
        {
            data.AddRange([0xFF, 0xFF, 0xFF, 0xFF]);
        }

        var hash = md5.ComputeHash(data.ToArray());

        if (_options.Revision >= 3)
        {
            for (var i = 0; i < 50; i++)
            {
                hash = md5.ComputeHash(hash.AsSpan(0, _keyLengthBytes).ToArray());
            }
        }

        return hash.AsSpan(0, _keyLengthBytes).ToArray();
    }

    private bool ValidateUserPassword(byte[] paddedPassword, byte[] fileKey)
    {
        using var md5 = MD5.Create();

        var data = new byte[paddedPassword.Length + _options.FileId.Length];
        paddedPassword.CopyTo(data, 0);
        _options.FileId.CopyTo(data, paddedPassword.Length);

        var hash = md5.ComputeHash(data);

        if (_options.Revision == 2)
        {
            var result = Rc4(fileKey, _passwordPadding);
            return result.AsSpan().SequenceEqual(_options.UserKey);
        }

        var encrypted = Rc4(fileKey, hash);
        for (var i = 1; i <= 19; i++)
        {
            encrypted = Rc4(XorKey(fileKey, i), encrypted);
        }

        return _options.UserKey.AsSpan(0, 16).SequenceEqual(encrypted.AsSpan(0, 16));
    }

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
                hash = md5.ComputeHash(hash.AsSpan(0, _keyLengthBytes).ToArray());
            }
        }

        var key = hash.AsSpan(0, _keyLengthBytes).ToArray();
        if (_options.Revision == 2)
        {
            return Rc4(key, _options.OwnerKey);
        }

        var result = _options.OwnerKey;
        for (var i = 19; i >= 0; i--)
        {
            result = Rc4(XorKey(key, i), result);
        }

        return result;
    }

    private bool ShouldEncryptMetadata(IStreamDictionary? streamDictionary)
    {
        if (_options.EncryptMetadata)
        {
            return true;
        }

        return streamDictionary?.Type != Constants.DictionaryTypes.Metadata;
    }

    private static byte[] PadPassword(string? password)
    {
        var bytes = string.IsNullOrEmpty(password)
            ? []
            : System.Text.Encoding.ASCII.GetBytes(password);

        var padded = new byte[32];
        var count = Math.Min(bytes.Length, padded.Length);
        Array.Copy(bytes, padded, count);
        if (count < padded.Length)
        {
            Array.Copy(_passwordPadding, 0, padded, count, padded.Length - count);
        }

        return padded;
    }

    private static byte[] DeriveObjectKey(IndirectObjectId objectId, byte[] fileKey)
    {
        using var md5 = MD5.Create();

        Span<byte> buffer = stackalloc byte[fileKey.Length + 5];
        fileKey.CopyTo(buffer);

        var index = objectId.Index;
        buffer[fileKey.Length] = (byte)(index & 0xFF);
        buffer[fileKey.Length + 1] = (byte)((index >> 8) & 0xFF);
        buffer[fileKey.Length + 2] = (byte)((index >> 16) & 0xFF);

        var generation = objectId.GenerationNumber;
        buffer[fileKey.Length + 3] = (byte)(generation & 0xFF);
        buffer[fileKey.Length + 4] = (byte)((generation >> 8) & 0xFF);

        var hash = md5.ComputeHash(buffer.ToArray());
        var keyLength = Math.Min(16, fileKey.Length + 5);
        return hash.AsSpan(0, keyLength).ToArray();
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
}
