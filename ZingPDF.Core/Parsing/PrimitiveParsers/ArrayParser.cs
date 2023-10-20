using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.ObjectGroups;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class ArrayParser : IPdfObjectParser<Objects.Primitives.Array>
    {
        public async ITask<Objects.Primitives.Array> ParseAsync(Stream stream)
        {
            // An array is a collection of any type of PDF object

            await stream.AdvanceToNextAsync(Constants.ArrayStart);

            var arrayStart = stream.Position;

            // Find end of array
            var content = string.Empty;
            int countStart = 0;
            int countEnd = 0;
            long arrayEnd = 0;

            do
            {
                int i = content.Length;
                content += await stream.GetAsync();

                for (; i < content.Length; i++)
                {
                    // TODO: consider if objects can contain escaped array delimiters which may break this logic, write tests

                    char c = content[i];

                    if (c == Constants.ArrayStart) { countStart++; }
                    if (c == Constants.ArrayEnd) { countEnd++; }

                    if (countStart > 0 && countEnd == countStart)
                    {
                        arrayEnd = i;
                        stream.Position = arrayStart + i;

                        await stream.AdvanceBeyondNextAsync(Constants.ArrayEnd);
                        break;
                    }
                }
            }
            while (stream.Position < stream.Length && countEnd != countStart);

            using var arrayStream = await stream.RangeAsync(arrayStart + 1, arrayEnd + arrayStart);

            var objectGroup = await Parser.For<PdfObjectGroup>().ParseAsync(arrayStream);

            return objectGroup.Objects.ToArray();
        }
    }
}