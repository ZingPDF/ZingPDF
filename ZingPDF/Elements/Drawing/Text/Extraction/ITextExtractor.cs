
namespace ZingPDF.Elements.Drawing.Text.Extraction
{
    public interface ITextExtractor
    {
        Task<IEnumerable<GlyphRun>> ExtractGlyphRunsAsync();
        Task<IEnumerable<ExtractedText>> ExtractTextAsync();
    }
}