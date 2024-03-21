using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Objects.Primitives;

namespace ZingPDF.Parsing.PrimitiveParsers
{
    internal class HexadecimalStringParser : IPdfObjectParser<HexadecimalString>
    {
        public async ITask<HexadecimalString> ParseAsync(Stream stream)
        {
            await stream.AdvanceBeyondNextAsync(Constants.LessThan);

            var content = await stream.ReadUpToIncludingAsync(Constants.GreaterThan);

            return content[..^1];
        }
    }
}
