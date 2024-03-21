using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Objects.Primitives;
using ZingPDF.Parsing;

namespace ZingPDF.Parsing.PrimitiveParsers
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
