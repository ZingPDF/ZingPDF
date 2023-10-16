using System.Runtime.ExceptionServices;
using ZingPdf.Core.Extensions;
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

            var xrefDictOffset = trailerObjects.Objects[2] as Integer;

            stream.Position = xrefDictOffset!;

            var xrefTable = await Parser.For<CrossReferenceTable>().ParseAsync(stream);

            //var indirectObjects = 

        }
    }
}
