namespace ZingPDF.OCR;

/// <summary>
/// Controls how OCR fallback behaves for a PDF.
/// </summary>
public sealed class PdfOcrOptions
{
    /// <summary>
    /// When true, existing embedded PDF text is returned before OCR is attempted.
    /// </summary>
    public bool PreferEmbeddedText { get; init; } = true;

    /// <summary>
    /// When true, pages without a supported OCR image candidate throw instead of returning an empty result.
    /// </summary>
    public bool ThrowWhenNoOcrCandidate { get; init; }
}
