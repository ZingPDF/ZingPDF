using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
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
