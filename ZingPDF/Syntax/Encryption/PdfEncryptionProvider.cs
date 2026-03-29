using Nito.AsyncEx;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Syntax.Encryption;

internal sealed class PdfEncryptionProvider : IPdfEncryptionProvider
{
    private readonly IPdf _pdf;
    private readonly AsyncLazy<StandardSecurityHandler?> _handler;
    private bool _authenticationAttempted;
    private bool _initializing;

    public PdfEncryptionProvider(IPdf pdf)
    {
        _pdf = pdf;
        _handler = new AsyncLazy<StandardSecurityHandler?>(CreateHandlerAsync);
    }

    public async Task AuthenticateAsync(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var handler = await _handler;
        if (handler is null)
        {
            return;
        }

        _authenticationAttempted = true;
        if (!handler.TryAuthenticate(password))
        {
            throw new PdfAuthenticationException("Invalid password for encrypted PDF.");
        }
    }

    public async Task<byte[]> DecryptObjectBytesAsync(ObjectContext context, byte[] data, IStreamDictionary? streamDictionary)
    {
        if (_initializing)
        {
            return data;
        }

        if (context.NearestParent is null)
        {
            return data;
        }

        var handler = await _handler;
        if (handler is null)
        {
            return data;
        }

        if (!handler.IsAuthenticated && !_authenticationAttempted)
        {
            _authenticationAttempted = true;
            handler.TryAuthenticate(string.Empty);
        }

        if (!handler.IsAuthenticated)
        {
            throw new PdfAuthenticationException("Invalid password for encrypted PDF.");
        }

        if (!handler.ShouldDecrypt(context.NearestParent.Id, streamDictionary))
        {
            return data;
        }

        return handler.Transform(context.NearestParent.Id, data, streamDictionary, encrypt: false);
    }

    public async Task<EncryptionWritePlan?> CreateWritePlanAsync(PdfEncryptionOptions? encryptionOptions = null)
    {
        if (encryptionOptions is not null)
        {
            return await CreateNewWritePlanAsync(encryptionOptions);
        }

        var handler = await _handler;
        if (handler is null)
        {
            return null;
        }

        if (!handler.IsAuthenticated && !_authenticationAttempted)
        {
            _authenticationAttempted = true;
            handler.TryAuthenticate(string.Empty);
        }

        if (!handler.IsAuthenticated)
        {
            throw new PdfAuthenticationException("Invalid password for encrypted PDF.");
        }

        var trailerDictionary = await _pdf.Objects.GetLatestTrailerDictionaryAsync();
        var encryptReference = trailerDictionary.GetAs<IndirectObjectReference>(Constants.DictionaryKeys.Trailer.Encrypt);

        return new EncryptionWritePlan(handler, encryptReference);
    }

    private async Task<StandardSecurityHandler?> CreateHandlerAsync()
    {
        _initializing = true;
        try
        {
            var trailerDict = await _pdf.Objects.GetLatestTrailerDictionaryAsync();
            var encryptionDict = await trailerDict.Encrypt.GetAsync();

            if (encryptionDict is null)
            {
                return null;
            }

            StandardEncryptionDictionary standardEncryption = encryptionDict is StandardEncryptionDictionary standard
                ? standard
                : StandardEncryptionDictionary.FromDictionary(encryptionDict.InnerDictionary, _pdf, encryptionDict.Context);

            var encryptRef = trailerDict.GetAs<IndirectObjectReference>(Constants.DictionaryKeys.Trailer.Encrypt);
            var fileId = ExtractFileId(trailerDict);

            return await CreateStandardSecurityHandlerAsync(standardEncryption, fileId, encryptRef?.Id);
        }
        finally
        {
            _initializing = false;
        }
    }

    private static byte[] ExtractFileId(ITrailerDictionary trailerDictionary)
    {
        var idArray = trailerDictionary.ID ?? throw new InvalidPdfException("Encrypted PDF is missing trailer ID.");
        var firstId = idArray.Get<IPdfObject>(0) ?? throw new InvalidPdfException("Encrypted PDF is missing trailer ID.");

        return firstId switch
        {
            PdfString pdfString => pdfString.Bytes,
            _ => throw new InvalidPdfException("Encrypted PDF trailer ID is not a string."),
        };
    }

    private static async Task<StandardSecurityHandler> CreateStandardSecurityHandlerAsync(
        StandardEncryptionDictionary standardEncryption,
        byte[] fileId,
        IndirectObjectId? encryptionDictionaryId)
    {
        var revision = (int)(await standardEncryption.R.GetAsync());
        var version = (int)(await standardEncryption.V.GetAsync());
        var filter = await standardEncryption.Filter.GetAsync();

        if (filter != "Standard")
        {
            throw new NotSupportedException($"Unsupported security handler: {filter}.");
        }

        var permissions = (int)(await standardEncryption.P.GetAsync());
        var keyLengthBits = (int?)(await standardEncryption.Length.GetAsync())
            ?? version switch
            {
                1 => 40,
                2 => 40,
                4 => 128,
                5 => 256,
                _ => 40,
            };
        var ownerString = await standardEncryption.O.GetAsync();
        var userString = await standardEncryption.U.GetAsync();
        var encryptMetadata = (await standardEncryption.EncryptMetadata.GetAsync())?.Value ?? true;
        var ownerEncryptionKey = (await standardEncryption.OE.GetAsync())?.Bytes;
        var userEncryptionKey = (await standardEncryption.UE.GetAsync())?.Bytes;
        var encryptedPermissions = (await standardEncryption.Perms.GetAsync())?.Bytes;
        var (stringMethod, streamMethod) = await ResolveCryptMethodsAsync(standardEncryption, version);

        var options = new StandardSecurityHandlerOptions(
            version,
            revision,
            keyLengthBits,
            permissions,
            ownerString.Bytes,
            userString.Bytes,
            encryptMetadata,
            fileId,
            stringMethod,
            streamMethod,
            ownerEncryptionKey,
            userEncryptionKey,
            encryptedPermissions,
            encryptionDictionaryId);

        return new StandardSecurityHandler(options);
    }

    private async Task<EncryptionWritePlan> CreateNewWritePlanAsync(PdfEncryptionOptions encryptionOptions)
    {
        var trailerDictionary = await _pdf.Objects.GetLatestTrailerDictionaryAsync();
        var originalFileId = GetOrCreateOriginalFileId(trailerDictionary);

        var initialOptions = StandardSecurityHandler.CreateNewOptions(
            encryptionOptions.UserPassword,
            encryptionOptions.OwnerPassword,
            originalFileId.Bytes,
            encryptionOptions.Permissions,
            encryptionOptions.Algorithm,
            encryptionOptions.EncryptMetadata);

        var encryptionDictionary = CreateStandardEncryptionDictionary(initialOptions);
        var encryptionIndirectObject = await _pdf.Objects.AddAsync(encryptionDictionary);

        var finalizedOptions = initialOptions with { EncryptionDictionaryId = encryptionIndirectObject.Id };
        var handler = new StandardSecurityHandler(finalizedOptions);
        if (!handler.TryAuthenticate(encryptionOptions.UserPassword))
        {
            throw new InvalidOperationException("Unable to initialize PDF encryption.");
        }

        return new EncryptionWritePlan(handler, encryptionIndirectObject.Reference, originalFileId);
    }

    private PdfString GetOrCreateOriginalFileId(ITrailerDictionary trailerDictionary)
    {
        if (trailerDictionary.ID?[0] is PdfString existingFileId)
        {
            return PdfString.FromBytes(existingFileId.Bytes, PdfStringSyntax.Hex, ObjectContext.UserCreated);
        }

        return PdfString.FromBytes(Guid.NewGuid().ToByteArray(), PdfStringSyntax.Hex, ObjectContext.UserCreated);
    }

    private StandardEncryptionDictionary CreateStandardEncryptionDictionary(StandardSecurityHandlerOptions options)
    {
        var dictionary = new Dictionary<string, IPdfObject>
        {
            [Constants.DictionaryKeys.Encryption.Filter] = (Name)"Standard",
            [Constants.DictionaryKeys.Encryption.V] = (Number)options.Version,
            [Constants.DictionaryKeys.Encryption.Standard.R] = (Number)options.Revision,
            [Constants.DictionaryKeys.Encryption.Standard.O] = PdfString.FromBytes(options.OwnerValue, PdfStringSyntax.Hex, ObjectContext.UserCreated),
            [Constants.DictionaryKeys.Encryption.Standard.U] = PdfString.FromBytes(options.UserValue, PdfStringSyntax.Hex, ObjectContext.UserCreated),
            [Constants.DictionaryKeys.Encryption.Standard.P] = (Number)options.Permissions,
        };

        if (options.KeyLengthBits != 40)
        {
            dictionary[Constants.DictionaryKeys.Encryption.Length] = (Number)options.KeyLengthBits;
        }

        if (!options.EncryptMetadata)
        {
            dictionary[Constants.DictionaryKeys.Encryption.Standard.EncryptMetadata] = BooleanObject.FromBool(false, ObjectContext.UserCreated);
        }

        if (options.Version >= 4)
        {
            dictionary[Constants.DictionaryKeys.Encryption.CF] = CreateCryptFiltersDictionary(options);
            dictionary[Constants.DictionaryKeys.Encryption.StmF] = (Name)Constants.Encryption.StandardCryptFilterName;
            dictionary[Constants.DictionaryKeys.Encryption.StrF] = (Name)Constants.Encryption.StandardCryptFilterName;
        }

        if (options.Version >= 5)
        {
            dictionary[Constants.DictionaryKeys.Encryption.Standard.OE] = PdfString.FromBytes(options.OwnerEncryptionKey!, PdfStringSyntax.Hex, ObjectContext.UserCreated);
            dictionary[Constants.DictionaryKeys.Encryption.Standard.UE] = PdfString.FromBytes(options.UserEncryptionKey!, PdfStringSyntax.Hex, ObjectContext.UserCreated);
            dictionary[Constants.DictionaryKeys.Encryption.Standard.Perms] = PdfString.FromBytes(options.EncryptedPermissions!, PdfStringSyntax.Hex, ObjectContext.UserCreated);
        }

        return StandardEncryptionDictionary.FromDictionary(dictionary, _pdf, ObjectContext.UserCreated);
    }

    private static async Task<(StandardCryptMethod StringMethod, StandardCryptMethod StreamMethod)> ResolveCryptMethodsAsync(
        StandardEncryptionDictionary standardEncryption,
        int version)
    {
        if (version <= 2)
        {
            return (StandardCryptMethod.Rc4, StandardCryptMethod.Rc4);
        }

        var cryptFilters = await standardEncryption.CF.GetAsync();
        var stringName = (await standardEncryption.StrF.GetAsync())?.Value ?? Constants.Encryption.StandardCryptFilterName;
        var streamName = (await standardEncryption.StmF.GetAsync())?.Value ?? Constants.Encryption.StandardCryptFilterName;

        return (
            ResolveCryptMethod(stringName, cryptFilters),
            ResolveCryptMethod(streamName, cryptFilters));
    }

    private static StandardCryptMethod ResolveCryptMethod(string name, ZingPDF.Syntax.Objects.Dictionaries.Dictionary? cryptFilters)
    {
        if (string.Equals(name, "Identity", StringComparison.Ordinal))
        {
            return StandardCryptMethod.Identity;
        }

        if (cryptFilters is null)
        {
            return name == Constants.Encryption.StandardCryptFilterName
                ? StandardCryptMethod.Rc4
                : throw new NotSupportedException($"Unsupported crypt filter '{name}'.");
        }

        var filterDictionary = cryptFilters.GetAs<ZingPDF.Syntax.Objects.Dictionaries.Dictionary>(name)
            ?? throw new NotSupportedException($"Unsupported crypt filter '{name}'.");
        var cfm = filterDictionary.GetAs<Name>(Constants.DictionaryKeys.Encryption.CryptFilter.CFM)?.Value;

        return cfm switch
        {
            "V2" => StandardCryptMethod.Rc4,
            "AESV2" => StandardCryptMethod.Aes128,
            "AESV3" => StandardCryptMethod.Aes256,
            _ => throw new NotSupportedException($"Unsupported crypt filter method '{cfm ?? "<missing>"}'."),
        };
    }

    private ZingPDF.Syntax.Objects.Dictionaries.Dictionary CreateCryptFiltersDictionary(StandardSecurityHandlerOptions options)
    {
        var cryptMethod = options.StreamMethod switch
        {
            StandardCryptMethod.Rc4 => "V2",
            StandardCryptMethod.Aes128 => "AESV2",
            StandardCryptMethod.Aes256 => "AESV3",
            _ => throw new NotSupportedException($"Unsupported stream crypt method '{options.StreamMethod}'."),
        };

        var cryptFilter = new ZingPDF.Syntax.Objects.Dictionaries.Dictionary(new Dictionary<string, IPdfObject>
        {
            [Constants.DictionaryKeys.Encryption.CryptFilter.CFM] = (Name)cryptMethod,
            [Constants.DictionaryKeys.Encryption.CryptFilter.AuthEvent] = (Name)"DocOpen",
            [Constants.DictionaryKeys.Encryption.Length] = (Number)(options.KeyLengthBits / 8),
        }, _pdf, ObjectContext.UserCreated);

        return new ZingPDF.Syntax.Objects.Dictionaries.Dictionary(new Dictionary<string, IPdfObject>
        {
            [Constants.Encryption.StandardCryptFilterName] = cryptFilter,
        }, _pdf, ObjectContext.UserCreated);
    }
}
