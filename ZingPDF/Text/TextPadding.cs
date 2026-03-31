namespace ZingPDF.Text;

/// <summary>
/// Padding inside a page text layout box.
/// </summary>
public readonly record struct TextPadding(double Left, double Top, double Right, double Bottom)
{
    public static TextPadding None => new(0, 0, 0, 0);

    public static TextPadding Default => Uniform(2);

    public static TextPadding Uniform(double value) => new(value, value, value, value);
}
