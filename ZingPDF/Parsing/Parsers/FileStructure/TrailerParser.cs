using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Parsing.Parsers.FileStructure
{
    internal class TrailerParser : IObjectParser<Trailer>
    {
        public async ITask<Trailer> ParseAsync(Stream stream)
        {
            await stream.AdvanceBeyondNextAsync(Constants.Trailer);

            var trailerObjects = await new PdfObjectGroupParser().ParseAsync(stream);

            var trailerDict = TrailerDictionary.FromDictionary(trailerObjects.Get<Dictionary>(0));

            var xrefTableOffset = trailerObjects.Get<Number>(2);

            return new Trailer(trailerDict, xrefTableOffset);
        }
    }
}
