using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class BooleanObjectParser : IPdfObjectParser<BooleanObject>
    {
        public async ITask<BooleanObject> ParseAsync(Stream stream)
        {
            stream.AdvancePastWhitepace();

            return bool.Parse(await stream.ReadUpToExcludingAsync(Constants.WhitespaceCharacters));
        }
    }
}
