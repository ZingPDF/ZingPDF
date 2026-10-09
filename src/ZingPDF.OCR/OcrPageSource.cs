namespace ZingPDF.OCR;

/// <summary>
/// Identifies the source that produced a page's returned text.
/// </summary>
public enum OcrPageSource
{
    /// <summary>No embedded text or OCR image produced text.</summary>
    None,

    /// <summary>Text came from the PDF's existing text objects.</summary>
    EmbeddedText,

    /// <summary>Text came from OCR of one supported image candidate.</summary>
    ImageCandidate,

    /// <summary>Text came from OCR of a rendered page image.</summary>
    RenderedPage
}
