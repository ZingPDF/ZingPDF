using MorseCode.ITask;
using System.Text;
using ZingPDF.Logging;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class ArrayParser : IObjectParser<ArrayObject>
    {
        private readonly IPdfContext _pdfContext;

        public ArrayParser(IPdfContext pdfContext)
        {
            _pdfContext = pdfContext;
        }

        public async ITask<ArrayObject> ParseAsync(Stream stream, ParseContext context)
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

                    if (c == Constants.Characters.LeftSquareBracket)
                    {
                        countStart++;
                        if (countStart == 1)
                        {
                            arrayStart = initialStreamPosition + i + 1;
                        }
                    }

                    if (c == Constants.Characters.RightSquareBracket)
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
                output = [];
            }
            else
            {
                // Create substream for parsing array contents
                var arrayStream = new SubStream(stream, arrayStart, arrayEnd);

                // Parse objects inside the array
                var objectGroup = await new PdfObjectGroupParser(_pdfContext).ParseAsync(arrayStream, context);

                output = new ArrayObject(objectGroup.Objects, ObjectOrigin.ParsedDocumentObject);
            }

            // Move the stream position past the array
            stream.Position = arrayEnd + 1;

            Logger.Log(LogLevel.Trace, $"Parsed ArrayObject between offsets: {initialStreamPosition} - {stream.Position}");

            return output;
        }

    }
}