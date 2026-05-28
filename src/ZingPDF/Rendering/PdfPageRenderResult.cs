namespace ZingPDF.Rendering;

/// <summary>
/// Contains the PNG bytes and geometry metadata produced by page rendering.
/// </summary>
public sealed record PdfPageRenderResult
{
    /// <summary>
    /// Gets the rendered 1-based page number.
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Gets the rendered image width in pixels.
    /// </summary>
    public required int PixelWidth { get; init; }

    /// <summary>
    /// Gets the rendered image height in pixels.
    /// </summary>
    public required int PixelHeight { get; init; }

    /// <summary>
    /// Gets the scale used for rendering.
    /// </summary>
    public required double Scale { get; init; }

    /// <summary>
    /// Gets the PDF page geometry used to size and map the rendered image.
    /// </summary>
    public required PdfPageGeometry Geometry { get; init; }

    /// <summary>
    /// Gets the rendered PNG image bytes.
    /// </summary>
    public required ReadOnlyMemory<byte> PngBytes { get; init; }
}
