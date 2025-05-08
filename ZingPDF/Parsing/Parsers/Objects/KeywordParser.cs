using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class KeywordParser : IObjectParser<Keyword>
    {
        private readonly IPdfContext _pdfContext;

        public KeywordParser(IPdfContext pdfContext)
        {
            _pdfContext = pdfContext;
        }

        public async ITask<Keyword> ParseAsync(Stream stream, ParseContext context)
        {
            stream.AdvancePastWhitepace();

            //Logger.Log(LogLevel.Trace, $"Parsing Keyword from {stream.GetType().Name} at offset: {stream.Position}.");

            var keyword = await stream.ReadUpToExcludingAsync(Constants.WhitespaceCharacters);

            Logger.Log(LogLevel.Trace, $"Parsed Keyword: {{{keyword}}}. {stream.GetType().Name} now at: {stream.Position}.");

            return new Keyword(keyword, context.Origin);
        }
    }
}
