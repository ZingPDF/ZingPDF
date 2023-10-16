using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    public class PdfParser
    {
        public async Task ParseAsync(Stream stream)
        {
            await new TrailerFinder().FindAsync(stream);

            await stream.AdvanceBeyondNextAsync(Constants.Trailer);

            var trailerObjects = await Parser.For<PdfObjectGroup>().ParseAsync(stream);

            var trailerDict = trailerObjects.Get<Dictionary>(0);
            var xrefDictOffset = trailerObjects.Get<Integer>(2);

            stream.Position = xrefDictOffset!;

            var xrefTable = await Parser.For<CrossReferenceTable>().ParseAsync(stream);

            var documentCatalogRef = trailerDict.Get<IndirectObjectReference>("Root");
            var documentCatalogOffset = xrefTable.IndirectObjectLocations[documentCatalogRef.Id];

            stream.Position = documentCatalogOffset;

            var documentCatalog = await Parser.For<Dictionary>().ParseAsync(stream);

            var pagesRef = documentCatalog.Get<IndirectObjectReference>("Pages");
            var pagesOffset = xrefTable.IndirectObjectLocations[pagesRef.Id];

            stream.Position = pagesOffset;

            var pagesCatalog = await Parser.For<Dictionary>().ParseAsync(stream);

            var pageRefs = pagesCatalog
                .Get<Objects.Primitives.Array>("Kids")
                .Cast<IndirectObjectReference>();

            List<Dictionary> pages = new();

            foreach(var pageRef in pageRefs)
            {
                var offset = xrefTable.IndirectObjectLocations[pageRef.Id];
                stream.Position = offset;

                pages.Add(await Parser.For<Dictionary>().ParseAsync(stream));
            }
        }
    }
}
