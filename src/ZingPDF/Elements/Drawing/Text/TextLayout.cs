namespace ZingPDF.Elements.Drawing.Text;

internal sealed record TextLayout
{
    public required double FontSize { get; init; }
    public required IReadOnlyList<TextLayoutSegment> Segments { get; init; }
}
