namespace ZingPDF.OCR;

/// <summary>
/// Describes the text returned for a single page when OCR fallback is used.
/// </summary>
public sealed class OcrPageResult
{
    public required int PageNumber { get; init; }
    public required string Text { get; init; }
    public bool UsedEmbeddedText { get; init; }
    public bool UsedOcr { get; init; }
    public string? SourceImageMimeType { get; init; }
    public int? SourceImageWidth { get; init; }
    public int? SourceImageHeight { get; init; }
}
