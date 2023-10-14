using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class IntegerParser : IPdfObjectParser<Integer>
    {
        public async ITask<Integer> ParseAsync(Stream stream)
        {
            await stream.AdvancePastWhitepaceAsync();

            var content = await stream.ReadUntilAsync(c => !c.IsInteger());

            content = content.TrimStart();

            return int.Parse(content);
        }
    }
}
