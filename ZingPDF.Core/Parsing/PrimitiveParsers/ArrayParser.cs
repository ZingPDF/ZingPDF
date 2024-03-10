using MorseCode.ITask;
using System.Text;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class ArrayParser : IPdfObjectParser<ArrayObject>
    {
        public async ITask<ArrayObject> ParseAsync(Stream stream)
        {
            // An array is a collection of any type of PDF object

            await stream.AdvanceBeyondNextAsync(Constants.ArrayStart);

            var arrayStart = stream.Position;

            var content = string.Empty;
            int countStart = 1;
            int countEnd = 0;

            var bufferSize = 1024;
            var buffer = new byte[bufferSize];

            do
            {
                int i = content.Length;
                _ = await stream.ReadAsync(buffer.AsMemory());

                content += Encoding.ASCII.GetString(buffer);

                for (; i < content.Length; i++)
                {
                    // TODO: consider if objects can contain escaped array delimiters which may break this logic, write tests

                    char c = content[i];

                    if (c == Constants.ArrayStart) { countStart++; }
                    if (c == Constants.ArrayEnd) { countEnd++; }

                    if (countEnd == countStart)
                    {
                        stream.Position = arrayStart + i - 1;

                        break;
                    }
                }
            }
            while (stream.Position < stream.Length && countEnd != countStart);

            var arrayEnd = stream.Position;

            ArrayObject output;

            if (arrayEnd - arrayStart < 1)
            {
                output = Array.Empty<PdfObject>();
            }
            else
            {
                var arrayStream = new SubStream(stream, arrayStart, arrayEnd)
                {
                    Position = 0
                };

                var objectGroup = await Parser.For<PdfObjectGroup>().ParseAsync(arrayStream);

                output = objectGroup.Objects.ToArray();
            }

            stream.Position = arrayEnd + 1;
            await stream.AdvanceBeyondNextAsync(Constants.ArrayEnd);

            return output;
        }
    }
}