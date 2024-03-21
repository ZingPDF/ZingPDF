using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Objects.Primitives;
using ZingPDF.Parsing;

namespace ZingPDF.Parsing.PrimitiveParsers
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
