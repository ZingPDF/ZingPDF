using MorseCode.ITask;
using System.Text;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class LiteralStringParser : IPdfObjectParser<LiteralString>
    {
        // Needs to be able to parse strings of various encodings.
        // N.B. In all encodings, the opening and closing parentheses are encoded using ASCII.
        // Examples - (café)
        // - ASCII      0x28 0x63 0x61 0x66 0x65 0x29 ('é' cannot be represented, so I'm substituting 'e')
        // - UTF8       0x28 0xEF 0xBB 0xBF 0x63 0x61 0x66 0xC3 0xA9 0x29 (Includes BOM (0xEF, 0xBB, 0xBF), 2 bytes are used to represent 'é')
        // - UTF16BE    0x28 0xFE 0xFF 0x00 0x63 0x00 0x61 0x00 0x66 0x00 0xE9 0x29 (Includes BOM (0xFE, 0xFF) 2 bytes are used for all characters)

        // - Octal      As above, but all characters outside of the ascii range are represented as octal codes, including the BOM.
        //              The string itself is encoded with ASCII in the PDF file.
        //      - e.g. (\376\377\000A\000r\000t\000i\000f\000e\000x)

        private static readonly string[] _escapeSequences = ["\\\\", "\\(", "\\)"];

        private readonly EncodingDetector _encodingDetector = new();

        public async ITask<LiteralString> ParseAsync(Stream stream)
        {
            //Logger.Log(LogLevel.Trace, $"Parsing literal string from {stream.GetType().Name} at offset: {stream.Position}.");

            await stream.AdvanceBeyondNextAsync(Constants.LeftParenthesis);

            var stringStart = stream.Position;

            // Find end of string
            var content = string.Empty;
            var asciiContent = string.Empty;
            int countStart = 1;
            int countEnd = 0;
            int stringEnd = 0;

            // We need to track any characters we remove.
            // After parsing, we'll need to move the stream to the end of the string.
            // We can't do this using the string content length, as it may have characters missing.
            // We'll add the correct number of bytes to the position by calculating the byte length of these characters.
            int removedChars = 0;

            static bool isEscapeSequence(string input) => _escapeSequences.Contains(input);

            var encodingResult = await _encodingDetector.DetectAsync(stream);
            var byteEncoding = encodingResult.IsOctal ? Encoding.ASCII : encodingResult.StringEncoding;

            var bufferSize = 1024;
            var buffer = new byte[bufferSize];

            do
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory());

                int i = content.Length;

                // Get the content using both its intended encoding AND ASCII.
                // If the string's encoding is UTF16BE then each character is represented by 2 bytes.
                // The closing parenthesis should have been encoded in ASCII which is 1 byte per character.
                // Therefore we can't use the UTF16BE interpreted content to find the closing parenthesis,
                // which will have been decoded incorrectly. e.g. ) is [0, 41] in UTFBE, but just [41] in ASCII.
                // In practice, the UTF16BE encoding grabs 2 bytes e.g. [41, 0] and interprets it as '⤀'.
                content += byteEncoding.GetString(buffer, 0, bytesRead);
                asciiContent += Encoding.ASCII.GetString(buffer, 0, bytesRead);

                // Used to track the position within the ascii string
                var asciiCursor = i;

                for (; i < content.Length; i++)
                {
                    var c = content[i];

                    var asciiChar = asciiContent[asciiCursor];
                    if (asciiChar == Constants.RightParenthesis)
                    {
                        countEnd++;
                    }

                    switch (c)
                    {
                        case Constants.LeftParenthesis:
                            countStart++;
                            break;
                        case Constants.ReverseSolidus:
                            // Backslash is used to:

                            // - escape parentheses
                            // - escape a backslash
                            if (i < content.Length - 2 && isEscapeSequence(content[i..(i + 2)]))
                            {
                                // Simply remove the slash
                                content = content.Remove(i, 1);
                                asciiContent = asciiContent.Remove(i, 1);

                                removedChars++;
                            }
                            // - split a string across multiple lines (ignore any end of line markers following the slash)
                            else if (i < content.Length - 1 && content[i + 1].IsEndOfLine())
                            {
                                // Remove the slash
                                content = content.Remove(i, 1);
                                asciiContent = asciiContent.Remove(i, 1);

                                removedChars++;

                                // ...and the EOL marker
                                content = content.RemoveNextEndOfLineMarker(out var removedEOLChars);
                                asciiContent = asciiContent.RemoveNextEndOfLineMarker(out _);

                                removedChars += byteEncoding.GetByteCount(removedEOLChars);
                            }
                            // - represent a 3 digit octal character code \005
                            else if (i < content.Length - 4 && content[(i + 1)..(i + 4)].IsInteger())
                            {
                                c = content[(i + 1)..(i + 4)].ToCharFromOctal();

                                content = content[..i] + c + content[(i + 4)..];
                                asciiContent = asciiContent[..i] + c + asciiContent[(i + 4)..];

                                removedChars += 4 - byteEncoding.GetByteCount([c]);
                            }
                            // - represent a 2 digit octal character code \53 (equivalent to \053)
                            else if (i < content.Length - 3 && content[(i + 1)..(i + 3)].IsInteger())
                            {
                                c = content[(i + 1)..(i + 3)].ToCharFromOctal();

                                content = content[..i] + c + content[(i + 3)..];
                                asciiContent = asciiContent[..i] + c + asciiContent[(i + 3)..];

                                removedChars += 3 - byteEncoding.GetByteCount([c]);
                            }
                            // - a single slash which is not part of an escape sequence is ignored
                            else
                            {
                                // Remove the slash
                                content = content.Remove(i, 1);
                                asciiContent = asciiContent.Remove(i, 1);

                                removedChars++;
                            }
                            break;
                    }

                    // Multibyte characters will take up multiple characters in the ascii string
                    // This counter allows us to skip to the next character next time.
                    asciiCursor += byteEncoding.GetByteCount([c]);

                    if (countStart > 0 && countEnd == countStart)
                    {
                        stringEnd = stringEnd = encodingResult.StringEncoding.BodyName != byteEncoding.BodyName
                            ? asciiCursor - 1
                            : i;

                        break;
                    }
                }
            }
            while (stream.Position < stream.Length && countEnd != countStart);

            var output = encodingResult.StringEncoding.BodyName != byteEncoding.BodyName
                ? asciiContent[..stringEnd]
                : content[..stringEnd];

            var preambleLength = encodingResult.IsOctal
                            ? encodingResult.StringEncoding.GetPreamble().Length * 4
                            : byteEncoding.GetPreamble().Length;

            stream.Position = stringStart
                + preambleLength
                + byteEncoding.GetByteCount(output)
                + removedChars;

            await stream.AdvanceBeyondNextAsync(Constants.RightParenthesis);

            // Octal strings are essentially double encoded, and written using ASCII
            // - once using the intended encoding (such as UTF16BE)
            // - then non-ascii characters are converted to octal codes
            // We have converted the octal codes back to the target encoding, which can now be decoded.
            if (encodingResult.IsOctal)
            {
                output = encodingResult.StringEncoding.GetString(Encoding.ASCII.GetBytes(output));
            }

            Logger.Log(LogLevel.Trace, $"Parsed LiteralString: {{{output}}}. {stream.GetType().Name} now at: {stream.Position}.");

            return new LiteralString(output, EnumFromEncoding(encodingResult.StringEncoding));
        }

        private static LiteralStringEncoding EnumFromEncoding(Encoding encoding)
        {
            if (encoding is UTF8Encoding)
            {
                return LiteralStringEncoding.UTF8;
            }
            else if (encoding is UnicodeEncoding u && u.CodePage == 1201)
            {
                return LiteralStringEncoding.UTF16BE;
            }
            else if (encoding.CodePage == 28591) // Latin 1
            {
                return LiteralStringEncoding.PDFDocEncoding;
            }

            throw new InvalidOperationException();
        }
    }
}
