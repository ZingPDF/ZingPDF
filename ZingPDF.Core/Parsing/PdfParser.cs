using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Parsing
{
    public class PdfParser
    {
        public static async Task<Pdf> ParseAsync(Stream stream)
        {
            var header = await new HeaderParser().ParseAsync(stream);

            // TODO: support parsing linearized files
            // TODO: check parsing multiple trailers for incremental updates works
            PdfObjectGroup trailerObjects = await GetTrailer(stream);

            var trailerDict = trailerObjects.Get<Dictionary>(0);

            CrossReferenceTable xrefTable = await GetCrossReferenceTable(stream, trailerObjects);

            var indirectObjectDereferencer = new IndirectObjectDereferencer();

            var documentCatalogReference = trailerDict.Get<IndirectObjectReference>("Root")
                ?? throw new ParserException("Trailer is missing an entry for `Root`");

            var infoReference = trailerDict.Get<IndirectObjectReference>("Info");
            var id = trailerDict.Get<ArrayObject>("ID");

            var body = await indirectObjectDereferencer.GetAllAsync(stream).ToListAsync();

            throw new NotImplementedException();
        }

        private static async Task<CrossReferenceTable> GetCrossReferenceTable(Stream stream, PdfObjectGroup trailerObjects)
        {
            var xrefDictOffset = trailerObjects.Get<Integer>(2);

            stream.Position = xrefDictOffset!;

            var xrefTable = await Parser.For<CrossReferenceTable>().ParseAsync(stream);
            return xrefTable;
        }

        private static async Task<PdfObjectGroup> GetTrailer(Stream stream)
        {
            await new TrailerFinder().FindAsync(stream);

            await stream.AdvanceBeyondNextAsync(Constants.Trailer);

            return await Parser.For<PdfObjectGroup>().ParseAsync(stream);
        }
    }
}
