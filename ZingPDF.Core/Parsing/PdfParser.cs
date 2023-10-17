using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    public class PdfParser
    {
        public async Task ParseAsync(Stream stream)
        {
            PdfObjectGroup trailerObjects = await GetTrailer(stream);

            var trailerDict = trailerObjects.Get<Dictionary>(0);

            CrossReferenceTable xrefTable = await GetCrossReferenceTable(stream, trailerObjects);

            var indirectObjectDereferencer = new IndirectObjectDereferencer(xrefTable);

            var documentCatalog = await indirectObjectDereferencer.GetSingle<Dictionary>(stream, trailerDict.Get<IndirectObjectReference>("Root"));
            var pagesCatalog = await indirectObjectDereferencer.GetSingle<Dictionary>(stream, documentCatalog.Get<IndirectObjectReference>("Pages"));

            List<Dictionary> pages = await GetPagesDictionaries(stream, xrefTable, pagesCatalog);
        }

        private static async Task<List<Dictionary>> GetPagesDictionaries(Stream stream, CrossReferenceTable xrefTable, Dictionary pagesCatalog)
        {
            var pageRefs = pagesCatalog
                .Get<Objects.Primitives.Array>("Kids")
                .Cast<IndirectObjectReference>();

            List<Dictionary> pages = new();

            foreach (var pageRef in pageRefs)
            {
                var offset = xrefTable.IndirectObjectLocations[pageRef.Id.Index];
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
            await new TrailerFinder().FindAsync(stream);

            await stream.AdvanceBeyondNextAsync(Constants.Trailer);

            return await Parser.For<PdfObjectGroup>().ParseAsync(stream);
        }
    }
}
