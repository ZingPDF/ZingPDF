using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class KeywordParser : IPdfObjectParser<Keyword>
    {
        public async ITask<Keyword> ParseAsync(Stream stream)
        {
            stream.AdvancePastWhitepace();

            return await stream.ReadUpToExcludingAsync(Constants.WhitespaceCharacters);
        }
    }
}
