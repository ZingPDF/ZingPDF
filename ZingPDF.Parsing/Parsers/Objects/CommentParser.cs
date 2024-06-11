using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class CommentParser : IPdfObjectParser<Comment>
    {
        public async ITask<Comment> ParseAsync(Stream stream)
        {
            await stream.AdvanceBeyondNextAsync(Constants.Percent);

            var value = await stream.ReadUpToExcludingAsync(Constants.EndOfLineCharacters);

            stream.AdvancePastWhitepace();

            return value;
        }
    }
}
