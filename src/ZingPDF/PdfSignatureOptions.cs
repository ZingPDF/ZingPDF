using System.Security.Cryptography;

namespace ZingPDF;

/// <summary>
/// Controls how a PDF signature dictionary is written before the CMS payload is embedded.
/// </summary>
public sealed class PdfSignatureOptions
{
    public string? FieldName { get; set; }

    public bool VisibleAppearance { get; set; } = true;

    public string? SignerName { get; set; }

    public string? Reason { get; set; }

    public string? Location { get; set; }

    public string? ContactInfo { get; set; }

    public DateTimeOffset? SigningDate { get; set; }

    public int EstimatedSignatureSizeBytes { get; set; } = 16384;

    public HashAlgorithmName DigestAlgorithm { get; set; } = HashAlgorithmName.SHA256;
}
