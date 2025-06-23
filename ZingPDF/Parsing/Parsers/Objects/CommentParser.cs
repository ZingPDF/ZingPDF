using Microsoft.Extensions.DependencyInjection;
using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class CommentParser : IParser<Comment>
    {
        public async ITask<Comment> ParseAsync(Stream stream, ParseContext context)
        {
            await stream.AdvanceBeyondNextAsync(Constants.Characters.Percent);

            var value = await stream.ReadUpToExcludingAsync(Constants.EndOfLineCharacters);

            stream.AdvancePastWhitepace();

            return new Comment(value, context.Origin);
        }
    }
}
