namespace ZingPDF.Text;

/// <summary>
/// High-level layout options for page text.
/// </summary>
public sealed record TextLayoutOptions
{
    /// <summary>
    /// The padding applied inside the supplied bounding box before layout is calculated.
    /// </summary>
    public TextPadding Padding { get; init; } = TextPadding.Default;

    /// <summary>
    /// How text should behave when it exceeds the available width.
    /// </summary>
    public TextOverflowMode Overflow { get; init; } = TextOverflowMode.Visible;

    /// <summary>
    /// Horizontal alignment inside the padded layout box.
    /// </summary>
    public TextHorizontalAlignment HorizontalAlignment { get; init; } = TextHorizontalAlignment.Start;

    /// <summary>
    /// Vertical alignment inside the padded layout box.
    /// </summary>
    public TextVerticalAlignment VerticalAlignment { get; init; } = TextVerticalAlignment.Middle;

    /// <summary>
    /// Reading direction hint used for start/end alignment.
    /// </summary>
    public TextReadingDirection ReadingDirection { get; init; } = TextReadingDirection.Auto;

    /// <summary>
    /// The minimum font size used by <see cref="TextOverflowMode.ShrinkToFit"/>.
    /// </summary>
    public double MinFontSize { get; init; } = 4;
}
