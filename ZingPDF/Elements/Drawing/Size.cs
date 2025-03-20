using ZingPDF.Syntax.Objects;

namespace ZingPDF.Elements.Drawing;

public class Size(Number width, Number height)
{
    public Number Width { get; set; } = width ?? throw new ArgumentNullException(nameof(width));
    public Number Height { get; set; } = height ?? throw new ArgumentNullException(nameof(height));

    public static Size Zero => new(0, 0);

    public override string ToString() => $"Width:{Width}, Height:{Height}";
}
