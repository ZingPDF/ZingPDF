using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Objects.Primitives;
using ZingPDF.Parsing;

namespace ZingPDF.Parsing.PrimitiveParsers
{
    internal class CommentParser : IPdfObjectParser<Comment>
    {
        public async ITask<Comment> ParseAsync(Stream stream)
        {
            await stream.AdvanceBeyondNextAsync(Constants.Comment);

            var value = await stream.ReadUpToExcludingAsync(Constants.EndOfLineCharacters);

            stream.AdvancePastWhitepace();

            return value;
        }
    }
}
