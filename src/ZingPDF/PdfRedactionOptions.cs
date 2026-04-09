using ZingPDF.Graphics;

namespace ZingPDF;

/// <summary>
/// Controls how pending redaction marks are applied to a PDF.
/// </summary>
public sealed class PdfRedactionOptions
{
    /// <summary>
    /// The fill colour used for redaction overlays.
    /// </summary>
    public RGBColour FillColor { get; init; } = RGBColour.Black;

    /// <summary>
    /// Optional overlay text drawn inside each redaction rectangle.
    /// </summary>
    public string? OverlayText { get; init; }

    /// <summary>
    /// The text colour used when <see cref="OverlayText"/> is set.
    /// </summary>
    public RGBColour OverlayTextColor { get; init; } = RGBColour.White;

    /// <summary>
    /// The standard PDF font used for overlay text.
    /// </summary>
    public string OverlayFontName { get; init; } = Fonts.StandardPdfFonts.HelveticaBold;

    /// <summary>
    /// The font size used for overlay text before any shrink-to-fit behavior.
    /// </summary>
    public double OverlayFontSize { get; init; } = 10;

}
