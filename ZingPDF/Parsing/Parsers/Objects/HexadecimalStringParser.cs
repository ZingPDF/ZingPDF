using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class HexadecimalStringParser : IPdfObjectParser<HexadecimalString>
    {
        public async ITask<HexadecimalString> ParseAsync(Stream stream)
        {
            //Logger.Log(LogLevel.Trace, $"Parsing Hexadecimal string from {stream.GetType().Name} at offset: {stream.Position}.");

            await stream.AdvanceBeyondNextAsync(Constants.LessThan);

            var content = await stream.ReadUpToIncludingAsync(Constants.GreaterThan);

            var value = content[..^1];

            Logger.Log(LogLevel.Trace, $"Parsed HexadecimalString: {{{value}}}. {stream.GetType().Name} now at: {stream.Position}.");

            return value;
        }
    }
}
