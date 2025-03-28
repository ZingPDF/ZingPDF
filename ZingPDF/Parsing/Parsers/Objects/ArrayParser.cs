using MorseCode.ITask;
using System.Text;
using ZingPDF.Logging;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class ArrayParser : IObjectParser<ArrayObject>
    {
        public async ITask<ArrayObject> ParseAsync(Stream stream)
        {
            //Logger.Log(LogLevel.Trace, $"Parsing ArrayObject from {stream.GetType().Name} at offset: {stream.Position}.");

            long initialStreamPosition = stream.Position;
            long arrayStart = 0;
            long arrayEnd = 0;

            var contentBuilder = new StringBuilder();
            int countStart = 0;
            int countEnd = 0;

            var buffer = new byte[1024];

            do
            {
                // Track where we are in content relative to previous buffer reads
                int contentOffset = contentBuilder.Length;

                // Read the next chunk of the stream
                int read = await stream.ReadAsync(buffer.AsMemory());
                if (read == 0) break; // EOF

                // Append the read content
                contentBuilder.Append(Encoding.ASCII.GetString(buffer, 0, read));
                var content = contentBuilder.ToString();

                // Parse characters for array delimiters
                for (int i = contentOffset; i < content.Length; i++)
                {
                    char c = content[i];

                    if (c == Constants.LeftSquareBracket)
                    {
                        countStart++;
                        if (countStart == 1)
                        {
                            arrayStart = initialStreamPosition + i + 1;
                        }
                    }

                    if (c == Constants.RightSquareBracket)
                    {
                        countEnd++;
                        if (countEnd == countStart)
                        {
                            arrayEnd = initialStreamPosition + i;
                            goto ReadyToParse; // Exit both loop and do-while
                        }
                    }
                }
            }
            while (countStart != countEnd && stream.Position < stream.Length);

        ReadyToParse:
            if (countStart != countEnd)
            {
                throw new ParserException("Mismatched array delimiters. PDF may be corrupt.");
            }

            ArrayObject output;

            // Determine array content
            if (arrayEnd - arrayStart <= 1)
            {
                output = Array.Empty<PdfObject>();
            }
            else
            {
                // Create substream for parsing array contents
                var arrayStream = new SubStream(stream, arrayStart, arrayEnd);

                // Parse objects inside the array
                // TODO: does this need a non-null IPdfEditor?
                var objectGroup = await new PdfObjectGroupParser().ParseAsync(arrayStream);

                output = objectGroup.Objects.ToArray();
            }

            // Move the stream position past the array
            stream.Position = arrayEnd + 1;

            Logger.Log(LogLevel.Trace, $"Parsed ArrayObject between offsets: {initialStreamPosition} - {stream.Position}");

            return output;
        }

    }
}