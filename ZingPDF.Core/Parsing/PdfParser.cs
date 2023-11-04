using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    public class PdfParser
    {
        public static async Task<Pdf> ParseAsync(Stream stream)
        {
            var header = await new HeaderParser().ParseAsync(stream);

            // TODO: support parsing multiple trailers for incremental updates
            PdfObjectGroup trailerObjects = await GetTrailer(stream);

            var trailerDict = trailerObjects.Get<Dictionary>(0);

            CrossReferenceTable xrefTable = await GetCrossReferenceTable(stream, trailerObjects);

            var indirectObjectDereferencer = new IndirectObjectDereferencer(xrefTable);

            var documentCatalogReference = trailerDict.Get<IndirectObjectReference>("Root")
                ?? throw new ParserException("Trailer is missing an entry for `Root`");

            var infoReference = trailerDict.Get<IndirectObjectReference>("Info");
            var id = trailerDict.Get<ArrayObject>("ID");

            var body = await indirectObjectDereferencer.GetAllAsync(stream).ToListAsync();

            return new Pdf(header, new[] { new PdfIncrement(body, xrefTable, documentCatalogReference, infoReference, id) });

            //var documentCatalog = await indirectObjectDereferencer.GetSingleAsync<Dictionary>(stream, documentCatalogId);
            //var pagesCatalog = await indirectObjectDereferencer.GetSingleAsync<Dictionary>(stream, documentCatalog.Get<IndirectObjectReference>("Pages")!);

            //// TODO: this parses pages as dictionaries (which they are, obvs).
            //// Do we need to parse them to proper Page objects which have the right properties?
            //List<Dictionary> pages = await GetPagesDictionaries(stream, xrefTable, pagesCatalog);

            //// TODO: THIS IS TEST CODE FOR NOW:
            //// can't use IndirectObjectDereferencer to get stream contents
            //// it contains 2 objects, dict and stream, we need the dict to get the length in order to parse this efficiently.
            //var contentsReference = pages.First().Get<IndirectObjectReference>("Contents")!;

            //var contentsStreamOffset = xrefTable.IndirectObjectLocations[contentsReference.Id.Index];

            //stream.Position = contentsStreamOffset;

            //await stream.AdvanceToNextAsync(Constants.DictionaryStart);

            //var contentsStreamObject = await Parser.For<StreamObject>().ParseAsync(stream);

            //var test = System.Text.Encoding.UTF8.GetString(await contentsStreamObject.DecodeAsync());
        }

        private static async Task<List<Dictionary>> GetPagesDictionaries(Stream stream, CrossReferenceTable xrefTable, Dictionary pagesCatalog)
        {
            var pageRefs = pagesCatalog
                .Get<ArrayObject>("Kids")!
                .Cast<IndirectObjectReference>();

            List<Dictionary> pages = new();

            foreach (var pageRef in pageRefs)
            {
                var offset = xrefTable.IndirectObjectLocations.Last(kvp => kvp.Key == pageRef.Id.Index).Value;
                stream.Position = offset;

                pages.Add(await Parser.For<Dictionary>().ParseAsync(stream));
            }

            return pages;
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
            // TODO: add support for finding multiple trailers (incremental updates)
            await new TrailerFinder().FindAsync(stream);

            await stream.AdvanceBeyondNextAsync(Constants.Trailer);

            return await Parser.For<PdfObjectGroup>().ParseAsync(stream);
        }
    }
}
