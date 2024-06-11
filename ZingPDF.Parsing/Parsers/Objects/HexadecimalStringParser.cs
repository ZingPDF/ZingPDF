using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
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
