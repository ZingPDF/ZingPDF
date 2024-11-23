using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class IntegerParser : IPdfObjectParser<Integer>
    {
        public async ITask<Integer> ParseAsync(Stream stream)
        {
            stream.AdvancePastWhitepace();

            //Logger.Log(LogLevel.Trace, $"Parsing Integer from {stream.GetType().Name} at offset: {stream.Position}.");

            var content = await stream.ReadUntilAsync(c => !c.IsInteger() && c != '-');

            content = content.TrimStart();

            var value = int.Parse(content);

            Logger.Log(LogLevel.Trace, $"Parsed Integer: {{{value}}}. {stream.GetType().Name} now at: {stream.Position}.");

            return value;
        }
    }
}
