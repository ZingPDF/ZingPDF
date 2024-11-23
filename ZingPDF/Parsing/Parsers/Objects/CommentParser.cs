using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class CommentParser : IPdfObjectParser<Comment>
    {
        public async ITask<Comment> ParseAsync(Stream stream)
        {
            //Logger.Log(LogLevel.Trace, $"Parsing Comment from {stream.GetType().Name} at offset: {stream.Position}.");

            await stream.AdvanceBeyondNextAsync(Constants.Percent);

            var value = await stream.ReadUpToExcludingAsync(Constants.EndOfLineCharacters);

            Logger.Log(LogLevel.Trace, $"Parsed Comment: {{{value}}}. {stream.GetType().Name} now at: {stream.Position}.");

            stream.AdvancePastWhitepace();

            return value;
        }
    }
}
