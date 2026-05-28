using ZingPDF.Graphics;

namespace ZingPDF.Rendering;

/// <summary>
/// Configures raster rendering for a PDF page.
/// </summary>
public sealed record PdfPageRenderOptions
{
    /// <summary>
    /// Gets the scale factor applied to PDF default user-space units.
    /// </summary>
    /// <remarks>
    /// A value of 1 renders 72 PDF units as 72 pixels. A value of 2 renders the same area at twice
    /// the pixel width and height. The value must be finite and greater than zero.
    /// </remarks>
    public double Scale { get; init; } = 1d;

    /// <summary>
    /// Gets whether page rotation should affect the output bitmap dimensions.
    /// </summary>
    public bool ApplyPageRotation { get; init; } = true;

    /// <summary>
    /// Gets whether the output should be sized from the page's visible crop/media intersection.
    /// </summary>
    public bool UseVisibleBox { get; init; } = true;

    /// <summary>
    /// Gets the background color used behind transparent page content.
    /// </summary>
    public RGBColour Background { get; init; } = RGBColour.White;

    internal void Validate()
    {
        if (!double.IsFinite(Scale) || Scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Scale), "Scale must be finite and greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(Background);
    }
}
