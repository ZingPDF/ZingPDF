namespace ZingPDF.OCR;

/// <summary>
/// Describes which page pixels were supplied to the OCR engine.
/// </summary>
/// <remarks>Coverage describes the input image area and does not guarantee that OCR recognized all visible text.</remarks>
public enum OcrInputCoverage
{
    /// <summary>No OCR image was supplied.</summary>
    None,

    /// <summary>A single supported image candidate was supplied; other page content may be absent.</summary>
    ImageCandidate,

    /// <summary>A rendered image of the full visible page was supplied.</summary>
    RenderedPage
}
