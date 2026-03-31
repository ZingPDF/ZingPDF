namespace ZingPDF.Text;

/// <summary>
/// Vertical alignment for single-line page text layout.
/// </summary>
public enum TextVerticalAlignment
{
    /// <summary>
    /// Align relative to the top of the layout box.
    /// </summary>
    Top,

    /// <summary>
    /// Center within the layout box while preserving ascent and descent space.
    /// </summary>
    Middle,

    /// <summary>
    /// Align relative to the bottom of the layout box.
    /// </summary>
    Bottom
}
