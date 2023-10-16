using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
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
