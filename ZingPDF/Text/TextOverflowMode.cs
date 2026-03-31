namespace ZingPDF.Text;

/// <summary>
/// Controls how page text behaves when it exceeds the available layout width.
/// </summary>
public enum TextOverflowMode
{
    /// <summary>
    /// Render the text without clipping. Overflow may extend outside the layout bounds.
    /// </summary>
    Visible,

    /// <summary>
    /// Clip the rendered text to the padded layout bounds.
    /// </summary>
    Clip,

    /// <summary>
    /// Reduce the font size until the text fits the available layout bounds, down to the configured minimum size.
    /// </summary>
    ShrinkToFit
}
