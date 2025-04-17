namespace ZingPDF.Elements.Drawing.Text.Extraction;

public record PositionedGlyph
{
    public string Character { get; init; } = default!;
    public float X { get; init; }
    public float Y { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
    public string FontName { get; init; } = default!;
    public float FontSize { get; init; }
    public int PageNumber { get; init; }
}
