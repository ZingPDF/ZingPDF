namespace ZingPDF.Elements.Drawing.Text.Extraction;

public class ExtractedText
{
    public required int PageNumber { get; init; }
    public required string Text { get; init; }
    public required string FontName { get; init; }
    public required double FontSize { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }

    //"width": 34.5,
    //"height": 12,
    //"rotation": 0
}
