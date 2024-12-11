using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class RealNumberParser : IPdfObjectParser<RealNumber>
    {
        public async ITask<RealNumber> ParseAsync(Stream stream, IIndirectObjectDictionary indirectObjectDictionary)
        {
            stream.AdvancePastWhitepace();

            //Logger.Log(LogLevel.Trace, $"Parsing real number from {stream.GetType().Name} at offset: {stream.Position}.");

            var content = await stream.ReadUpToExcludingAsync([..Constants.Delimiters, ..Constants.WhitespaceCharacters]);

            var value = double.Parse(content);

            Logger.Log(LogLevel.Trace, $"Parsed RealNumber: {{{value}}}. {stream.GetType().Name} now at: {stream.Position}.");

            return value;
        }
    }
}
