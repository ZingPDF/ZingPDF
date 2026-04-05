
namespace ZingPDF.Elements.Drawing.Text.Extraction
{
    public interface ITextExtractor
    {
        Task<IEnumerable<GlyphRun>> ExtractGlyphRunsAsync();
        Task<IEnumerable<ExtractedText>> ExtractTextAsync();
        Task<IEnumerable<ExtractedText>> ExtractTextAsync(int pageNumber);
        Task<TextExtractionResult> ExtractTextAsync(TextExtractionOptions options);
        Task<TextExtractionResult> ExtractTextAsync(int pageNumber, TextExtractionOptions options);
    }
}
