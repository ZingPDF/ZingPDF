using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class BooleanObjectParser : IPdfObjectParser<BooleanObject>
    {
        public async ITask<BooleanObject> ParseAsync(Stream stream)
        {
            await stream.AdvancePastWhitepaceAsync();

            return bool.Parse(await stream.ReadUpToExcludingAsync(Constants.WhitespaceCharacters));
        }
    }
}
