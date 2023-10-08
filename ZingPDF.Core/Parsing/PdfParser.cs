using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Parsing
{
    public class PdfParser
    {
        public async Task ParseAsync(Stream stream)
        {
            var trailerContent = await new TrailerFinder().FindAsync(stream);

            var trailer = Parser.For<Trailer>().Parse(trailerContent);
        }
    }
}
