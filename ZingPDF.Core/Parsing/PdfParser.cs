namespace ZingPdf.Core.Parsing
{
    public class PdfParser
    {
        public async Task ParseAsync(Stream stream)
        {
            await new TrailerFinder().FindAsync(stream);

            var trailerObjects = PdfContentParser.ParseAsync(stream);

        }
    }
}
