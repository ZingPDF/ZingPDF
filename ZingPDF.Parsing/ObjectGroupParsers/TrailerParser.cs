using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.ObjectModel.FileStructure.Trailer;
using ZingPDF.ObjectModel.Objects;

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
