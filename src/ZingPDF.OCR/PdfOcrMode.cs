namespace ZingPDF.OCR;

/// <summary>
/// Selects the page image supplied to the OCR engine.
/// </summary>
public enum PdfOcrMode
{
    /// <summary>
    /// OCRs the largest supported image XObject found in the page resources.
    /// This preserves the package's original behavior and does not cover all page content.
    /// </summary>
    ImageCandidate,

    /// <summary>
    /// Renders the page's visible crop box, including page rotation and all rendered content, then OCRs that image.
    /// </summary>
    RenderedPage
}
