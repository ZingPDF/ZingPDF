using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Parsing.Parsers.Objects.Dictionaries;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.FileStructure
{
    internal class TrailerParser : IObjectParser<Trailer>
    {
        public async ITask<Trailer> ParseAsync(Stream stream)
        {
            await stream.AdvanceBeyondNextAsync(Constants.Trailer);

            //var trailerObjects = await new PdfObjectGroupParser().ParseAsync(stream);

            //var trailerDict = TrailerDictionary.FromDictionary(trailerObjects.Get<Dictionary>(0));
            var trailerDict = TrailerDictionary.FromDictionary(await new ComplexDictionaryParser(EmptyPdfEditor.Instance).ParseAsync(stream));

            _ = await Parser.For<Keyword>().ParseAsync(stream); // startxref
            var xrefTableOffset = await Parser.For<Number>().ParseAsync(stream);

            return new Trailer(trailerDict, xrefTableOffset);
        }
    }
}
