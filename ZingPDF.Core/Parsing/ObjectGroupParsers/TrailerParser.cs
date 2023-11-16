using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.ObjectGroupParsers
{
    internal class TrailerParser : IPdfObjectParser<Trailer>
    {
        public async ITask<Trailer> ParseAsync(Stream stream)
        {
            await stream.AdvanceBeyondNextAsync(Constants.Trailer);

            var trailerObjects = await Parser.For<PdfObjectGroup>().ParseAsync(stream);

            var trailerDict = TrailerDictionary.FromDictionary(trailerObjects.Get<Dictionary>(0));

            var xrefTableOffset = trailerObjects.Get<Integer>(2);

            return new Trailer(trailerDict, xrefTableOffset);
        }
    }
}
