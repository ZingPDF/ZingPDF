namespace ZingPDF.Elements.Drawing;

public class Size(double width, double height)
{
    public double Width { get; } = width;
    public double Height { get; } = height;

    public static Size Zero => new(0, 0);

    public override string ToString() => $"Width:{Width}, Height:{Height}";
}
