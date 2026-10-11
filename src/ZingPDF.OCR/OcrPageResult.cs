namespace ZingPDF.OCR;

/// <summary>
/// Describes the text returned for a single page when OCR fallback is used.
/// </summary>
public sealed class OcrPageResult
{
    /// <summary>Gets the 1-based page number.</summary>
    public required int PageNumber { get; init; }
    /// <summary>Gets recognized or extracted text for this page.</summary>
    public required string Text { get; init; }
    /// <summary>Gets whether the returned text came from embedded PDF text.</summary>
    public bool UsedEmbeddedText { get; init; }
    /// <summary>Gets whether an OCR engine processed an image.</summary>
    public bool UsedOcr { get; init; }
    /// <summary>Gets the source used to produce <see cref="Text"/>.</summary>
    public OcrPageSource Source { get; init; }
    /// <summary>Gets the area represented by the OCR input image, if OCR ran.</summary>
    /// <remarks>This describes image coverage only; it does not certify text recognition completeness.</remarks>
    public OcrInputCoverage InputCoverage { get; init; }
    /// <summary>Gets the MIME type of the image supplied to the OCR engine, when OCR ran.</summary>
    public string? SourceImageMimeType { get; init; }
    /// <summary>Gets the width in pixels of the image supplied to the OCR engine, when OCR ran.</summary>
    public int? SourceImageWidth { get; init; }
    /// <summary>Gets the height in pixels of the image supplied to the OCR engine, when OCR ran.</summary>
    public int? SourceImageHeight { get; init; }
}
