namespace ZingPDF.Rendering;

/// <summary>
/// Represents a failure while rasterizing a PDF page.
/// </summary>
public sealed class PdfRenderException : Exception
{
    public PdfRenderException(string message)
        : base(message)
    {
    }

    public PdfRenderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
