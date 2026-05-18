using System.Security.Cryptography.X509Certificates;

namespace ZingPDF;

/// <summary>
/// Describes a signed byte range in a PDF file.
/// </summary>
public sealed record PdfByteRange(long Offset, long Length);

/// <summary>
/// Selects the overall validation policy used by <see cref="PdfSignature.ValidateAsync"/>.
/// </summary>
public enum PdfSignatureValidationProfile
{
    /// <summary>
    /// Verifies PDF byte ranges and the detached CMS payload without certificate-chain, revocation, or PDF permission policy checks.
    /// </summary>
    IntegrityOnly,

    /// <summary>
    /// Verifies integrity and builds the signer certificate chain.
    /// </summary>
    CertificateChain,

    /// <summary>
    /// Verifies integrity, builds the certificate chain, and enables revocation checks when configured.
    /// </summary>
    LongTermValidation,

    /// <summary>
    /// Enables all validation layers currently supported by ZingPDF and reports unsupported layers explicitly.
    /// </summary>
    Strict
}

/// <summary>
/// Controls certificate-chain validation for PDF signatures.
/// </summary>
public enum PdfCertificateValidationMode
{
    None,
    BuildChain
}

/// <summary>
/// Controls revocation checking for signer certificates.
/// </summary>
public enum PdfSignatureRevocationMode
{
    None,
    Offline,
    Online
}

/// <summary>
/// Controls which time is used when validating certificate chains.
/// </summary>
public enum PdfSignatureValidationTimeMode
{
    Now,
    ClaimedSigningTime,
    TrustedSigningTimeThenClaimedSigningTimeThenNow
}

/// <summary>
/// Status for one validation layer.
/// </summary>
public enum PdfSignatureCheckStatus
{
    NotChecked,
    Valid,
    Warning,
    Invalid,
    Indeterminate,
    Unsupported
}

/// <summary>
/// Overall status for a PDF signature validation operation.
/// </summary>
public enum PdfSignatureValidationStatus
{
    Valid,
    ValidWithWarnings,
    Invalid,
    Indeterminate,
    Unsupported
}

/// <summary>
/// Severity for a validation finding.
/// </summary>
public enum PdfSignatureFindingSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Configures PDF signature validation.
/// </summary>
public sealed class PdfSignatureValidationOptions
{
    public PdfSignatureValidationProfile Profile { get; set; } = PdfSignatureValidationProfile.IntegrityOnly;

    public PdfCertificateValidationMode CertificateValidation { get; set; } = PdfCertificateValidationMode.None;

    public PdfSignatureRevocationMode RevocationMode { get; set; } = PdfSignatureRevocationMode.None;

    public PdfSignatureValidationTimeMode ValidationTimeMode { get; set; } = PdfSignatureValidationTimeMode.Now;

    public X509Certificate2Collection? ExtraCertificates { get; set; }

    public X509Certificate2Collection? TrustedRoots { get; set; }

    public bool AllowOnlineRevocationChecks { get; set; }
}

/// <summary>
/// A validation finding emitted by one validation layer.
/// </summary>
public sealed record PdfSignatureValidationFinding(
    PdfSignatureFindingSeverity Severity,
    string Code,
    string Message);

/// <summary>
/// Metadata from a PDF signature dictionary.
/// </summary>
public sealed record PdfSignatureMetadata(
    string? Filter,
    string? SubFilter,
    string? SignerName,
    string? Reason,
    string? Location,
    string? ContactInfo,
    DateTimeOffset? ClaimedSigningTime);

/// <summary>
/// Result of deterministic PDF byte-range and CMS verification.
/// </summary>
public sealed class PdfSignatureIntegrityResult
{
    public PdfSignatureCheckStatus Status { get; init; }

    public bool ByteRangeValid { get; init; }

    public bool CmsSignatureValid { get; init; }

    public bool MessageDigestValid { get; init; }

    public string? DigestAlgorithm { get; init; }

    public X509Certificate2? SignerCertificate { get; init; }
}

/// <summary>
/// Result of signature byte-range coverage checks.
/// </summary>
public sealed class PdfSignatureCoverageResult
{
    public PdfSignatureCheckStatus Status { get; init; }

    public bool CoversEntireDocumentAtSigningRevision { get; init; }

    public bool HasUnsignedChangesAfterSignature { get; init; }

    public long SignedLength { get; init; }

    public long FileLength { get; init; }
}

/// <summary>
/// Result of signer certificate-chain validation.
/// </summary>
public sealed class PdfSignatureCertificateResult
{
    public PdfSignatureCheckStatus Status { get; init; }

    public X509Certificate2? SignerCertificate { get; init; }

    public IReadOnlyList<X509Certificate2> ChainCertificates { get; init; } = [];

    public IReadOnlyList<string> ChainErrors { get; init; } = [];
}

/// <summary>
/// Result of revocation validation.
/// </summary>
public sealed class PdfSignatureRevocationResult
{
    public PdfSignatureCheckStatus Status { get; init; }

    public bool CheckedOnline { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Result of signing-time and timestamp validation.
/// </summary>
public sealed class PdfSignatureTimestampResult
{
    public PdfSignatureCheckStatus Status { get; init; }

    public DateTimeOffset? ClaimedSigningTime { get; init; }

    public DateTimeOffset? TrustedSigningTime { get; init; }

    public bool HasTrustedTimestamp { get; init; }
}

/// <summary>
/// Result of PDF certification and modification-permission validation.
/// </summary>
public sealed class PdfSignaturePermissionResult
{
    public PdfSignatureCheckStatus Status { get; init; }

    public bool HasDocMdpTransform { get; init; }

    public bool ChangesAllowedByCertificationSignature { get; init; }
}

/// <summary>
/// Full result for a PDF signature validation operation.
/// </summary>
public sealed class PdfSignatureValidationResult
{
    public PdfSignatureValidationStatus Status { get; init; }

    public IReadOnlyList<PdfSignatureValidationFinding> Findings { get; init; } = [];

    public PdfSignatureIntegrityResult Integrity { get; init; } = new();

    public PdfSignatureCoverageResult Coverage { get; init; } = new();

    public PdfSignatureCertificateResult Certificate { get; init; } = new();

    public PdfSignatureRevocationResult Revocation { get; init; } = new();

    public PdfSignatureTimestampResult Timestamp { get; init; } = new();

    public PdfSignaturePermissionResult Permissions { get; init; } = new();
}
