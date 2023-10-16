using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class KeywordParser : IPdfObjectParser<Keyword>
    {
        public async ITask<Keyword> ParseAsync(Stream stream)
        {
            await stream.AdvancePastWhitepaceAsync();

            return await stream.ReadUpToExcludingAsync(Constants.WhitespaceCharacters);
        }
    }
}
