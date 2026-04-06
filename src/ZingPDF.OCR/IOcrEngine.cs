namespace ZingPDF.OCR;

/// <summary>
/// Recognizes text from a page image supplied by the OCR package.
/// </summary>
public interface IOcrEngine
{
    /// <summary>
    /// Recognizes text from the supplied image.
    /// </summary>
    Task<string> RecognizeAsync(OcrInputImage image, CancellationToken cancellationToken = default);
}
