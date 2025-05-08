using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax.FileStructure.Trailer;

namespace ZingPDF.Parsing.Parsers.FileStructure
{
    internal class TrailerParser : IObjectParser<Trailer>
    {
        private IPdfContext _pdfContext;

        public TrailerParser(IPdfContext pdfContext)
        {
            _pdfContext = pdfContext;
        }

        public async ITask<Trailer> ParseAsync(Stream stream, ParseContext context)
        {
            await stream.AdvanceBeyondNextAsync(Constants.Trailer);

            var trailerDict = TrailerDictionary.FromDictionary(await _pdfContext.Parser.Dictionaries.ParseAsync(stream, context));

            _ = await _pdfContext.Parser.Keywords.ParseAsync(stream, context); // startxref
            var xrefTableOffset = await _pdfContext.Parser.Numbers.ParseAsync(stream, context);

            return new Trailer(trailerDict, xrefTableOffset, context.Origin);
        }
    }
}
