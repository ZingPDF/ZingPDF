using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.FileStructure
{
    internal class TrailerParser : IPdfObjectParser<Trailer>
    {
        public async ITask<Trailer> ParseAsync(Stream stream, IIndirectObjectDictionary indirectObjectDictionary)
        {
            await stream.AdvanceBeyondNextAsync(Constants.Trailer);

            var trailerObjects = await Parser.PdfObjectGroups.ParseAsync(stream, indirectObjectDictionary);

            var trailerDict = TrailerDictionary.FromDictionary(trailerObjects.Get<Dictionary>(0));

            var xrefTableOffset = trailerObjects.Get<Integer>(2);

            return new Trailer(trailerDict, xrefTableOffset);
        }
    }
}
