using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using ZingPDF.Elements.Forms.FieldTypes.Signature;
using ZingPDF.Extensions;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF;

/// <summary>
/// Represents a signed PDF signature field and exposes validation operations for that signature.
/// </summary>
public sealed class PdfSignature
{
    private readonly Pdf _pdf;
    private readonly SignatureFormField _field;
    private readonly Dictionary _signatureDictionary;

    internal PdfSignature(Pdf pdf, SignatureFormField field, Dictionary signatureDictionary)
    {
        _pdf = pdf ?? throw new ArgumentNullException(nameof(pdf));
        _field = field ?? throw new ArgumentNullException(nameof(field));
        _signatureDictionary = signatureDictionary ?? throw new ArgumentNullException(nameof(signatureDictionary));

        FieldName = field.Name;
        Metadata = new PdfSignatureMetadata(
            _signatureDictionary.GetAs<Name>("Filter")?.Value,
            _signatureDictionary.GetAs<Name>("SubFilter")?.Value,
            _signatureDictionary.GetAs<PdfString>("Name")?.Decode(),
            _signatureDictionary.GetAs<PdfString>("Reason")?.Decode(),
            _signatureDictionary.GetAs<PdfString>("Location")?.Decode(),
            _signatureDictionary.GetAs<PdfString>("ContactInfo")?.Decode(),
            _signatureDictionary.GetAs<Date>("M")?.DateTimeOffset);
    }

    /// <summary>
    /// Gets the fully qualified signature field name.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Gets metadata stored in the signature dictionary.
    /// </summary>
    public PdfSignatureMetadata Metadata { get; }

    /// <summary>
    /// Validates deterministic PDF byte-range and CMS integrity only.
    /// </summary>
    public Task<PdfSignatureValidationResult> ValidateIntegrityAsync()
        => ValidateAsync(new PdfSignatureValidationOptions());

    /// <summary>
    /// Validates this signature using the requested validation layers.
    /// </summary>
    public async Task<PdfSignatureValidationResult> ValidateAsync(PdfSignatureValidationOptions? options = null)
    {
        options = NormalizeOptions(options);
        var findings = new List<PdfSignatureValidationFinding>();
        var pdfBytes = await ReadPdfBytesAsync(_pdf.Data);

        var (integrity, coverage, cms) = ValidateIntegrity(pdfBytes, findings);
        var timestamp = ValidateTimestamp(options, findings);
        var certificate = ValidateCertificate(options, cms, timestamp, findings);
        var revocation = ValidateRevocation(options, certificate, findings);
        var permissions = ValidatePermissions(options, findings);

        return new PdfSignatureValidationResult
        {
            Status = DeriveOverallStatus(integrity, coverage, certificate, revocation, timestamp, permissions),
            Findings = findings,
            Integrity = integrity,
            Coverage = coverage,
            Certificate = certificate,
            Revocation = revocation,
            Timestamp = timestamp,
            Permissions = permissions
        };
    }

    internal static async Task<PdfSignature?> CreateAsync(Pdf pdf, SignatureFormField field)
    {
        var signatureDictionary = await field.GetSignatureDictionaryForValidationAsync();
        return signatureDictionary is null ? null : new PdfSignature(pdf, field, signatureDictionary);
    }

    private (PdfSignatureIntegrityResult Integrity, PdfSignatureCoverageResult Coverage, SignedCms? Cms) ValidateIntegrity(
        byte[] pdfBytes,
        List<PdfSignatureValidationFinding> findings)
    {
        if (!TryReadByteRange(out var byteRanges, out var byteRangeFailure))
        {
            findings.Add(new(PdfSignatureFindingSeverity.Error, "ByteRange.Invalid", byteRangeFailure));
            return (
                new PdfSignatureIntegrityResult { Status = PdfSignatureCheckStatus.Invalid },
                new PdfSignatureCoverageResult { Status = PdfSignatureCheckStatus.Invalid, FileLength = pdfBytes.Length },
                null);
        }

        if (!ValidateByteRanges(byteRanges, pdfBytes.Length, out var signedLength, out var byteRangeValidationFailure))
        {
            findings.Add(new(PdfSignatureFindingSeverity.Error, "ByteRange.Invalid", byteRangeValidationFailure));
            return (
                new PdfSignatureIntegrityResult { Status = PdfSignatureCheckStatus.Invalid },
                new PdfSignatureCoverageResult { Status = PdfSignatureCheckStatus.Invalid, FileLength = pdfBytes.Length },
                null);
        }

        var signedContent = new byte[signedLength];
        var writeOffset = 0;
        foreach (var range in byteRanges)
        {
            Buffer.BlockCopy(pdfBytes, checked((int)range.Offset), signedContent, writeOffset, checked((int)range.Length));
            writeOffset += checked((int)range.Length);
        }

        var coverage = new PdfSignatureCoverageResult
        {
            Status = byteRanges[^1].Offset + byteRanges[^1].Length == pdfBytes.Length
                ? PdfSignatureCheckStatus.Valid
                : PdfSignatureCheckStatus.Warning,
            CoversEntireDocumentAtSigningRevision = true,
            HasUnsignedChangesAfterSignature = byteRanges[^1].Offset + byteRanges[^1].Length < pdfBytes.Length,
            SignedLength = byteRanges[^1].Offset + byteRanges[^1].Length,
            FileLength = pdfBytes.Length
        };

        if (coverage.HasUnsignedChangesAfterSignature)
        {
            findings.Add(new(PdfSignatureFindingSeverity.Warning, "Coverage.UnsignedChangesAfterSignature", "The PDF contains bytes after the signed revision."));
        }

        var contents = _signatureDictionary.GetAs<PdfString>("Contents")?.Bytes;
        if (contents is null || contents.Length == 0)
        {
            findings.Add(new(PdfSignatureFindingSeverity.Error, "Contents.Missing", "The signature dictionary does not contain a CMS payload."));
            return (
                new PdfSignatureIntegrityResult { Status = PdfSignatureCheckStatus.Invalid, ByteRangeValid = true },
                coverage,
                null);
        }

        byte[] cmsBytes;
        try
        {
            AsnDecoder.ReadEncodedValue(contents, AsnEncodingRules.BER, out _, out _, out var consumed);
            cmsBytes = contents.AsSpan(0, consumed).ToArray();
        }
        catch (Exception ex) when (ex is AsnContentException or ArgumentException)
        {
            findings.Add(new(PdfSignatureFindingSeverity.Error, "Contents.InvalidCms", "The signature Contents value is not a valid CMS payload."));
            return (
                new PdfSignatureIntegrityResult { Status = PdfSignatureCheckStatus.Invalid, ByteRangeValid = true },
                coverage,
                null);
        }

        var cms = new SignedCms(new ContentInfo(signedContent), detached: true);
        try
        {
            cms.Decode(cmsBytes);
        }
        catch (CryptographicException)
        {
            findings.Add(new(PdfSignatureFindingSeverity.Error, "Contents.InvalidCms", "The signature Contents value could not be decoded as CMS."));
            return (
                new PdfSignatureIntegrityResult { Status = PdfSignatureCheckStatus.Invalid, ByteRangeValid = true },
                coverage,
                null);
        }

        var messageDigestValid = false;
        var cmsSignatureValid = false;
        try
        {
            cms.CheckHash();
            messageDigestValid = true;
        }
        catch (CryptographicException)
        {
            findings.Add(new(PdfSignatureFindingSeverity.Error, "Integrity.MessageDigestMismatch", "The CMS message digest does not match the signed PDF byte ranges."));
        }

        try
        {
            cms.CheckSignature(verifySignatureOnly: true);
            cmsSignatureValid = true;
        }
        catch (CryptographicException)
        {
            findings.Add(new(PdfSignatureFindingSeverity.Error, "Integrity.SignatureMismatch", "The CMS signature could not be verified against the signer certificate."));
        }

        var signer = cms.SignerInfos.Count > 0 ? cms.SignerInfos[0] : null;
        var signerCertificate = signer?.Certificate;

        return (
            new PdfSignatureIntegrityResult
            {
                Status = messageDigestValid && cmsSignatureValid ? PdfSignatureCheckStatus.Valid : PdfSignatureCheckStatus.Invalid,
                ByteRangeValid = true,
                MessageDigestValid = messageDigestValid,
                CmsSignatureValid = cmsSignatureValid,
                DigestAlgorithm = signer?.DigestAlgorithm.Value,
                SignerCertificate = signerCertificate
            },
            coverage,
            cms);
    }

    private PdfSignatureCertificateResult ValidateCertificate(
        PdfSignatureValidationOptions options,
        SignedCms? cms,
        PdfSignatureTimestampResult timestamp,
        List<PdfSignatureValidationFinding> findings)
    {
        if (options.CertificateValidation == PdfCertificateValidationMode.None)
        {
            return new PdfSignatureCertificateResult
            {
                Status = PdfSignatureCheckStatus.NotChecked,
                SignerCertificate = cms?.SignerInfos.Count > 0 ? cms.SignerInfos[0].Certificate : null
            };
        }

        if (cms is null || cms.SignerInfos.Count == 0)
        {
            findings.Add(new(PdfSignatureFindingSeverity.Error, "Certificate.MissingSigner", "The CMS payload does not contain a signer certificate."));
            return new PdfSignatureCertificateResult { Status = PdfSignatureCheckStatus.Invalid };
        }

        var signerCertificate = cms.SignerInfos[0].Certificate;
        if (signerCertificate is null)
        {
            findings.Add(new(PdfSignatureFindingSeverity.Error, "Certificate.MissingSigner", "The CMS payload does not contain a signer certificate."));
            return new PdfSignatureCertificateResult { Status = PdfSignatureCheckStatus.Invalid };
        }

        using var chain = new X509Chain();
        chain.ChainPolicy.VerificationTime = ResolveValidationTime(options, timestamp).UtcDateTime;
        chain.ChainPolicy.RevocationMode = options.RevocationMode switch
        {
            PdfSignatureRevocationMode.None => X509RevocationMode.NoCheck,
            PdfSignatureRevocationMode.Offline => X509RevocationMode.Offline,
            PdfSignatureRevocationMode.Online when options.AllowOnlineRevocationChecks => X509RevocationMode.Online,
            PdfSignatureRevocationMode.Online => X509RevocationMode.NoCheck,
            _ => X509RevocationMode.NoCheck
        };

        if (options.ExtraCertificates is not null)
        {
            chain.ChainPolicy.ExtraStore.AddRange(options.ExtraCertificates);
        }

        chain.ChainPolicy.ExtraStore.AddRange(cms.Certificates);

        if (options.TrustedRoots is { Count: > 0 })
        {
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.AddRange(options.TrustedRoots);
        }

        var valid = chain.Build(signerCertificate);
        var errors = chain.ChainStatus.Select(status => $"{status.Status}: {status.StatusInformation.Trim()}").ToList();
        if (!valid)
        {
            findings.AddRange(errors.Select(error => new PdfSignatureValidationFinding(
                PdfSignatureFindingSeverity.Error,
                "Certificate.ChainError",
                error)));
        }

        if (options.RevocationMode == PdfSignatureRevocationMode.Online && !options.AllowOnlineRevocationChecks)
        {
            findings.Add(new(
                PdfSignatureFindingSeverity.Warning,
                "Revocation.OnlineDisabled",
                "Online revocation checks were requested but AllowOnlineRevocationChecks is false."));
        }

        return new PdfSignatureCertificateResult
        {
            Status = valid ? PdfSignatureCheckStatus.Valid : PdfSignatureCheckStatus.Invalid,
            SignerCertificate = signerCertificate,
            ChainCertificates = chain.ChainElements.Cast<X509ChainElement>().Select(element => element.Certificate).ToList(),
            ChainErrors = errors
        };
    }

    private PdfSignatureRevocationResult ValidateRevocation(
        PdfSignatureValidationOptions options,
        PdfSignatureCertificateResult certificate,
        List<PdfSignatureValidationFinding> findings)
    {
        if (options.RevocationMode == PdfSignatureRevocationMode.None)
        {
            return new PdfSignatureRevocationResult { Status = PdfSignatureCheckStatus.NotChecked };
        }

        if (options.CertificateValidation == PdfCertificateValidationMode.None)
        {
            findings.Add(new(PdfSignatureFindingSeverity.Warning, "Revocation.RequiresCertificateValidation", "Revocation status is reported through certificate-chain validation."));
            return new PdfSignatureRevocationResult { Status = PdfSignatureCheckStatus.NotChecked };
        }

        var revocationErrors = certificate.ChainErrors
            .Where(error => error.Contains("Revocation", StringComparison.OrdinalIgnoreCase)
                || error.Contains("revocation", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new PdfSignatureRevocationResult
        {
            Status = revocationErrors.Count == 0 && certificate.Status == PdfSignatureCheckStatus.Valid
                ? PdfSignatureCheckStatus.Valid
                : revocationErrors.Count > 0 ? PdfSignatureCheckStatus.Invalid : PdfSignatureCheckStatus.Indeterminate,
            CheckedOnline = options.RevocationMode == PdfSignatureRevocationMode.Online && options.AllowOnlineRevocationChecks,
            Errors = revocationErrors
        };
    }

    private PdfSignatureTimestampResult ValidateTimestamp(
        PdfSignatureValidationOptions options,
        List<PdfSignatureValidationFinding> findings)
    {
        if (options.ValidationTimeMode == PdfSignatureValidationTimeMode.TrustedSigningTimeThenClaimedSigningTimeThenNow)
        {
            findings.Add(new(
                PdfSignatureFindingSeverity.Warning,
                "Timestamp.TrustedTimestampUnsupported",
                "Trusted timestamp token validation is not implemented yet; claimed signing time or current time will be used."));

            return new PdfSignatureTimestampResult
            {
                Status = Metadata.ClaimedSigningTime is null ? PdfSignatureCheckStatus.Unsupported : PdfSignatureCheckStatus.Warning,
                ClaimedSigningTime = Metadata.ClaimedSigningTime,
                TrustedSigningTime = null,
                HasTrustedTimestamp = false
            };
        }

        return new PdfSignatureTimestampResult
        {
            Status = Metadata.ClaimedSigningTime is null ? PdfSignatureCheckStatus.NotChecked : PdfSignatureCheckStatus.Valid,
            ClaimedSigningTime = Metadata.ClaimedSigningTime,
            TrustedSigningTime = null,
            HasTrustedTimestamp = false
        };
    }

    private static PdfSignaturePermissionResult ValidatePermissions(
        PdfSignatureValidationOptions options,
        List<PdfSignatureValidationFinding> findings)
    {
        if (options.Profile != PdfSignatureValidationProfile.Strict)
        {
            return new PdfSignaturePermissionResult { Status = PdfSignatureCheckStatus.NotChecked };
        }

        findings.Add(new(
            PdfSignatureFindingSeverity.Warning,
            "Permissions.DocMdpUnsupported",
            "DocMDP and certification permission validation is not implemented yet."));

        return new PdfSignaturePermissionResult
        {
            Status = PdfSignatureCheckStatus.Unsupported,
            HasDocMdpTransform = false,
            ChangesAllowedByCertificationSignature = false
        };
    }

    private bool TryReadByteRange(out IReadOnlyList<PdfByteRange> byteRanges, out string failure)
    {
        byteRanges = [];
        failure = string.Empty;

        if (_signatureDictionary.GetAs<ArrayObject>("ByteRange") is not { } byteRangeArray)
        {
            failure = "The signature dictionary does not contain a ByteRange array.";
            return false;
        }

        var numbers = byteRangeArray.OfType<Number>().Select(number => (long)number.Value).ToList();
        if (numbers.Count != 4)
        {
            failure = "The signature ByteRange array must contain exactly four numbers.";
            return false;
        }

        byteRanges = [new PdfByteRange(numbers[0], numbers[1]), new PdfByteRange(numbers[2], numbers[3])];
        return true;
    }

    private static bool ValidateByteRanges(
        IReadOnlyList<PdfByteRange> byteRanges,
        long fileLength,
        out int signedLength,
        out string failure)
    {
        signedLength = 0;
        failure = string.Empty;

        if (byteRanges.Count != 2)
        {
            failure = "Only two-part PDF signature ByteRange arrays are supported.";
            return false;
        }

        var previousEnd = 0L;
        foreach (var range in byteRanges)
        {
            if (range.Offset < 0 || range.Length < 0)
            {
                failure = "The signature ByteRange contains a negative offset or length.";
                return false;
            }

            var end = range.Offset + range.Length;
            if (end < range.Offset || end > fileLength)
            {
                failure = "The signature ByteRange extends beyond the PDF file length.";
                return false;
            }

            if (range.Offset < previousEnd)
            {
                failure = "The signature ByteRange entries overlap or are out of order.";
                return false;
            }

            checked
            {
                signedLength += (int)range.Length;
            }

            previousEnd = end;
        }

        return true;
    }

    private static DateTimeOffset ResolveValidationTime(PdfSignatureValidationOptions options, PdfSignatureTimestampResult timestamp)
    {
        return options.ValidationTimeMode switch
        {
            PdfSignatureValidationTimeMode.ClaimedSigningTime => timestamp.ClaimedSigningTime ?? DateTimeOffset.UtcNow,
            PdfSignatureValidationTimeMode.TrustedSigningTimeThenClaimedSigningTimeThenNow => timestamp.TrustedSigningTime ?? timestamp.ClaimedSigningTime ?? DateTimeOffset.UtcNow,
            _ => DateTimeOffset.UtcNow
        };
    }

    private static PdfSignatureValidationOptions NormalizeOptions(PdfSignatureValidationOptions? options)
    {
        options ??= new PdfSignatureValidationOptions();
        switch (options.Profile)
        {
            case PdfSignatureValidationProfile.CertificateChain:
                if (options.CertificateValidation == PdfCertificateValidationMode.None)
                {
                    options.CertificateValidation = PdfCertificateValidationMode.BuildChain;
                }
                break;
            case PdfSignatureValidationProfile.LongTermValidation:
                options.CertificateValidation = PdfCertificateValidationMode.BuildChain;
                if (options.RevocationMode == PdfSignatureRevocationMode.None)
                {
                    options.RevocationMode = PdfSignatureRevocationMode.Offline;
                }
                break;
            case PdfSignatureValidationProfile.Strict:
                options.CertificateValidation = PdfCertificateValidationMode.BuildChain;
                if (options.RevocationMode == PdfSignatureRevocationMode.None)
                {
                    options.RevocationMode = PdfSignatureRevocationMode.Offline;
                }
                options.ValidationTimeMode = PdfSignatureValidationTimeMode.TrustedSigningTimeThenClaimedSigningTimeThenNow;
                break;
        }

        return options;
    }

    private static PdfSignatureValidationStatus DeriveOverallStatus(params object[] results)
    {
        var statuses = results.Select(result => result switch
        {
            PdfSignatureIntegrityResult value => value.Status,
            PdfSignatureCoverageResult value => value.Status,
            PdfSignatureCertificateResult value => value.Status,
            PdfSignatureRevocationResult value => value.Status,
            PdfSignatureTimestampResult value => value.Status,
            PdfSignaturePermissionResult value => value.Status,
            _ => PdfSignatureCheckStatus.NotChecked
        }).ToList();

        if (statuses.Contains(PdfSignatureCheckStatus.Invalid))
        {
            return PdfSignatureValidationStatus.Invalid;
        }

        if (statuses.Contains(PdfSignatureCheckStatus.Indeterminate))
        {
            return PdfSignatureValidationStatus.Indeterminate;
        }

        if (statuses.Contains(PdfSignatureCheckStatus.Unsupported))
        {
            return PdfSignatureValidationStatus.Unsupported;
        }

        if (statuses.Contains(PdfSignatureCheckStatus.Warning))
        {
            return PdfSignatureValidationStatus.ValidWithWarnings;
        }

        return PdfSignatureValidationStatus.Valid;
    }

    private static async Task<byte[]> ReadPdfBytesAsync(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new NotSupportedException("Signature validation requires a seekable PDF input stream.");
        }

        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            return copy.ToArray();
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }
}
