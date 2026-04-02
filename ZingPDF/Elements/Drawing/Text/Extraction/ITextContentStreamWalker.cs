namespace ZingPDF.Elements.Drawing.Text.Extraction;

internal interface ITextContentStreamWalker
{
    Task<List<TextRun>> ExtractTextRunsAsync(Stream stream, TextDrawingState state, int pageNumber);
}
