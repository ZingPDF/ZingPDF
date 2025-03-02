using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class NumberParser : IObjectParser<Number>
    {
        public async ITask<Number> ParseAsync(Stream stream)
        {
            stream.AdvancePastWhitepace(); 

            //Logger.Log(LogLevel.Trace, $"Parsing Integer from {stream.GetType().Name} at offset: {stream.Position}.");

            var content = await stream.ReadUpToExcludingAsync([.. Constants.Delimiters, .. Constants.WhitespaceCharacters]);

            content = content.TrimStart();

            var value = double.Parse(content);

            Logger.Log(LogLevel.Trace, $"Parsed Number: {{{value}}}. {stream.GetType().Name} now at: {stream.Position}.");

            return value;
        }
    }
}
