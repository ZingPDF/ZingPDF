using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class RealNumberParser : IPdfObjectParser<RealNumber>
    {
        public async ITask<RealNumber> ParseAsync(Stream stream)
        {
            await stream.AdvancePastWhitepaceAsync();

            var content = await stream.ReadUpToExcludingAsync(Constants.WhitespaceCharacters);

            return double.Parse(content);
        }
    }
}
