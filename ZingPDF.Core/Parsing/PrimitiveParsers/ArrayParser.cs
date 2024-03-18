using MorseCode.ITask;
using System.Text;
using ZingPdf.Core.Logging;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class ArrayParser : IPdfObjectParser<ArrayObject>
    {
        public async ITask<ArrayObject> ParseAsync(Stream stream)
        {
            Logger.Log(Logging.LogLevel.Trace, $"Parsing ArrayObject from {stream.GetType().Name} at offset: {stream.Position}.");

            // An array is a collection of any type of PDF object

            var initialStreamPosition = stream.Position;
            var arrayStart = 0L;
            var arrayEnd = 0L;

            var content = string.Empty;
            int countStart = 0;
            int countEnd = 0;

            var bufferSize = 1024;
            var buffer = new byte[bufferSize];

            do
            {
                int i = content.Length;
                var read = await stream.ReadAsync(buffer.AsMemory());

                content += Encoding.ASCII.GetString(buffer, 0, read);

                for (; i < content.Length; i++)
                {
                    // TODO: consider if objects can contain escaped array delimiters which may break this logic, write tests

                    char c = content[i];

                    if (c == Constants.ArrayStart)
                    {
                        countStart++;

                        if (countStart == 1)
                        {
                            arrayStart = initialStreamPosition + i + 1;
                        }
                    }

                    if (c == Constants.ArrayEnd)
                    {
                        countEnd++;

                        if (countEnd == countStart)
                        {
                            // TODO: this is used to build a substream, and move past the array
                            //      but i is a character count, not a byte count. Use the proper byte length of the content.

                            arrayEnd = initialStreamPosition + i;

                            break;
                        }
                    }
                }
            }
            while (countStart != countEnd && stream.Position < stream.Length);

            ArrayObject output;

            if (arrayEnd - arrayStart <= 1)
            {
                output = Array.Empty<PdfObject>();
            }
            else
            {
                var arrayStream = new SubStream(stream, arrayStart, arrayEnd);

                var objectGroup = await Parser.For<PdfObjectGroup>().ParseAsync(arrayStream);

                output = objectGroup.Objects.ToArray();
            }

            stream.Position = arrayEnd + 1;

            Logger.Log(LogLevel.Trace, $"Parsed ArrayObject between offsets: {initialStreamPosition} - {stream.Position}");

            return output;
        }
    }
}