using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Objects.ObjectGroups;
using ZingPDF.Objects.ObjectGroups.Trailer;
using ZingPDF.Objects.Primitives;

namespace ZingPDF.Parsing.ObjectGroupParsers
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
