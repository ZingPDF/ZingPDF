using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class HexadecimalStringParser : IObjectParser<HexadecimalString>
    {
        private IPdfContext _pdfContext;

        public HexadecimalStringParser(IPdfContext pdfContext)
        {
            _pdfContext = pdfContext;
        }

        public async ITask<HexadecimalString> ParseAsync(Stream stream, ParseContext context)
        {
            //Logger.Log(LogLevel.Trace, $"Parsing Hexadecimal string from {stream.GetType().Name} at offset: {stream.Position}.");

            await stream.AdvanceBeyondNextAsync(Constants.Characters.LessThan);

            var content = await stream.ReadUpToIncludingAsync(Constants.Characters.GreaterThan);

            var value = content[..^1];

            Logger.Log(LogLevel.Trace, $"Parsed HexadecimalString: {{{value}}}. {stream.GetType().Name} now at: {stream.Position}.");

            return HexadecimalString.FromHexString(value, context.Origin);
        }
    }
}
