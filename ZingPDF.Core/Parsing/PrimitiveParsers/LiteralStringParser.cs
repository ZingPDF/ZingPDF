using MorseCode.ITask;
using System.Text;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class LiteralStringParser : IPdfObjectParser<LiteralString>
    {
        private readonly string[] _escapeSequences = new[] { "\\\\", "\\(", "\\)" };

        public async ITask<LiteralString> ParseAsync(Stream stream)
        {
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
            List<char> removedChars = new();

            var isEscapeSequence = (string input) => _escapeSequences.Contains(input);

            var encoding = DetectEncoding(stream);

            var bufferSize = 1024;
            var buffer = new byte[bufferSize];

            do
            {
                _ = await stream.ReadAsync(buffer.AsMemory());

                int i = content.Length;

                // Get the content using both its intended encoding AND ASCII.
                // If the string's encoding is UTF16BE then each character is represented by 2 bytes.
                // The closing parenthesis should have been encoded in ASCII which is 1 byte per character.
                // Therefore we can't use the UTF16BE interpreted content to find the closing parenthesis,
                // which will have been decoded incorrectly. e.g. ) is [0, 41] in UTFBE, but just [41] in ASCII.
                // In practice, the UTF16BE encoding grabs 2 bytes e.g. [41, 0] and interprets it as '⤀'.
                content += encoding.GetString(buffer);
                asciiContent += Encoding.ASCII.GetString(buffer).Replace("\0", "");

                for (; i < asciiContent.Length; i++)
                {
                    var c = content[i];
                    var asciiChar = asciiContent[i];

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
                            if (i < asciiContent.Length - 1 && isEscapeSequence(content[i..(i + 2)]))
                            {
                                // Simply remove the slash
                                content = content.Remove(i, 1);
                                asciiContent = asciiContent.Remove(i, 1);

                                removedChars.Add(Constants.ReverseSolidus);
                            }
                            // - split a string across multiple lines (ignore any end of line markers following the slash)
                            else if (i < content.Length - 1 && content[i + 1].IsEndOfLine())
                            {
                                // Remove the slash
                                content = content.Remove(i, 1);
                                asciiContent = asciiContent.Remove(i, 1);

                                removedChars.Add(Constants.ReverseSolidus);

                                // ...and the EOL marker
                                content = content.RemoveNextEndOfLineMarker(out var removedEOLChars);
                                asciiContent = asciiContent.RemoveNextEndOfLineMarker(out _);

                                removedChars.AddRange(removedEOLChars);
                            }
                            // - represent a 3 digit octal character code \005
                            else if (i < content.Length - 4 && content[(i + 1)..(i + 4)].IsInteger())
                            {
                                removedChars.AddRange(content[i..(i + 4)]);

                                var octalAsChar = content[(i + 1)..(i + 4)].ToCharFromOctal();

                                content = content[..i] + octalAsChar + content[(i + 4)..];
                                asciiContent = asciiContent[..i] + octalAsChar + asciiContent[(i + 4)..];
                            }
                            // - represent a 2 digit octal character code \53 (equivalent to \053)
                            else if (i < content.Length - 3 && content[(i + 1)..(i + 3)].IsInteger())
                            {
                                removedChars.AddRange(content[i..(i + 3)]);

                                var octalAsChar = content[(i + 1)..(i + 3)].ToCharFromOctal();

                                content = content[..i] + octalAsChar + content[(i + 3)..];
                                asciiContent = asciiContent[..i] + octalAsChar + asciiContent[(i + 3)..];
                            }
                            // - a single slash which is not part of an escape sequence is ignored
                            else
                            {
                                // Remove the slash
                                content = content.Remove(i, 1);
                                asciiContent = asciiContent.Remove(i, 1);

                                removedChars.Add(Constants.ReverseSolidus);
                            }
                            break;
                    }

                    if (countStart > 0 && countEnd == countStart)
                    {
                        stringEnd = i;
                        stream.Position = stringStart
                            + encoding.GetPreamble().Length
                            + encoding.GetByteCount(content[..stringEnd])
                            + encoding.GetByteCount(removedChars.ToArray());

                        await stream.AdvanceBeyondNextAsync(Constants.RightParenthesis);
                        break;
                    }
                }
            }
            while (stream.Position < stream.Length && countEnd != countStart);

            return new LiteralString(content[..stringEnd], EnumFromEncoding(encoding));
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

        private static Encoding DetectEncoding(Stream stream)
        {
            var startPosition = stream.Position;

            var utf8Preamble = new byte[] { 239, 187, 191 };
            var utf16bePreamble = new byte[] { 254, 255 };

            var firstBytes = new byte[3];
            int read = stream.Read(firstBytes, 0, firstBytes.Length);

            if (read == 3)
            {
                if (firstBytes[0] == utf8Preamble[0]
                    && firstBytes[1] == utf8Preamble[1]
                    && firstBytes[2] == utf8Preamble[2])
                {
                    return Encoding.UTF8;
                }
                else if (firstBytes[0] == utf16bePreamble[0]
                    && firstBytes[1] == utf16bePreamble[1])
                {
                    stream.Position = startPosition + 2;
                    return Encoding.BigEndianUnicode;
                }
            }

            stream.Position = startPosition;
            return Encoding.Latin1; // PDFDocEncoding
        }
    }
}
