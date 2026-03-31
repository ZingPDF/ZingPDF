namespace ZingPDF.Text;

/// <summary>
/// Reading direction hint for page text layout.
/// </summary>
public enum TextReadingDirection
{
    /// <summary>
    /// Infer the reading direction from the text where possible.
    /// </summary>
    Auto,

    /// <summary>
    /// Treat the text as left-to-right.
    /// </summary>
    LeftToRight,

    /// <summary>
    /// Treat the text as right-to-left for layout alignment purposes.
    /// </summary>
    RightToLeft
}
