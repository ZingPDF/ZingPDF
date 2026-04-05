namespace ZingPDF.Elements.Drawing.Text.Extraction;

public class GlyphRun
{
    public int PageNumber { get; }
    public IReadOnlyList<PositionedGlyph> Glyphs { get; }

    public GlyphRun(int pageNumber, IReadOnlyList<PositionedGlyph> glyphs)
    {
        PageNumber = pageNumber;
        Glyphs = glyphs;
    }
}
