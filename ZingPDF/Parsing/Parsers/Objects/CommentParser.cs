using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class CommentParser : IObjectParser<Comment>
    {
        private readonly IPdfContext _pdfContext;

        public CommentParser(IPdfContext pdfContext)
        {
            _pdfContext = pdfContext;
        }

        public async ITask<Comment> ParseAsync(Stream stream, ParseContext context)
        {
            //Logger.Log(LogLevel.Trace, $"Parsing Comment from {stream.GetType().Name} at offset: {stream.Position}.");

            await stream.AdvanceBeyondNextAsync(Constants.Characters.Percent);

            var value = await stream.ReadUpToExcludingAsync(Constants.EndOfLineCharacters);

            Logger.Log(LogLevel.Trace, $"Parsed Comment: {{{value}}}. {stream.GetType().Name} now at: {stream.Position}.");

            stream.AdvancePastWhitepace();

            return new Comment(value, context.Origin);
        }
    }
}
