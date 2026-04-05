namespace ZingPDF.Elements.Drawing.Text.Extraction;

public record PositionedGlyph
{
    public required char Character { get; init; }
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Width { get; init; }
    public required float Height { get; init; }
    public required string FontName { get; init; }
    public required float FontSize { get; init; }
    public required int PageNumber { get; init; }
}
