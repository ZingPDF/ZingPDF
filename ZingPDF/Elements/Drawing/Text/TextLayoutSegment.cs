namespace ZingPDF.Elements.Drawing.Text;

internal sealed record TextLayoutSegment
{
    public required string Text { get; init; }
    public required Coordinate Origin { get; init; }
}
