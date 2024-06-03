using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.Parsing.PrimitiveParsers
{
    internal class IntegerParser : IPdfObjectParser<Integer>
    {
        public async ITask<Integer> ParseAsync(Stream stream)
        {
            stream.AdvancePastWhitepace();

            var content = await stream.ReadUntilAsync(c => !c.IsInteger() && c != '-');

            content = content.TrimStart();

            return int.Parse(content);
        }
    }
}
