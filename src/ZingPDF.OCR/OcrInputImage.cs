namespace ZingPDF.OCR;

/// <summary>
/// Represents a page image prepared for OCR.
/// </summary>
public sealed class OcrInputImage
{
    public required int PageNumber { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string MimeType { get; init; }
    public required byte[] Data { get; init; }
}
