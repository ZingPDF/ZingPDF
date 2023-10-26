using MorseCode.ITask;
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

            var stringStart = stream.Position - 1;

            // Find end of string
            var content = string.Empty;
            int countStart = 1;
            int countEnd = 0;
            int stringEnd = 0;

            var isEscapeSequence = (string input) => _escapeSequences.Contains(input);

            var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);

            var buffer = new char[1024];
            while ((_ = await reader.ReadBlockAsync(buffer.AsMemory())) > 0 && countEnd != countStart)
            {
                int i = content.Length;

                content += new string(buffer);

                for (; i < content.Length; i++)
                {
                    var c = content[i];

                    switch (c)
                    {
                        case Constants.LeftParenthesis:
                            countStart++;
                            break;
                        case Constants.RightParenthesis:
                            countEnd++;
                            break;
                        case Constants.ReverseSolidus:
                            // Backslash is used to:

                            // - escape parentheses
                            // - escape a backslash
                            if (i < content.Length - 1 && isEscapeSequence(content[i..(i + 2)]))
                            {
                                // Simply remove the slash
                                content = content.Remove(i, 1);
                            }
                            // - split a string across multiple lines (ignore any end of line markers following the slash)
                            else if (i < content.Length - 1 && content[i + 1].IsEndOfLine())
                            {
                                // Remove the slash
                                content = content.Remove(i, 1);
                                // ...and the EOL marker
                                content = content.RemoveNextEndOfLineMarker();
                            }
                            // - represent a 3 digit octal character code \005
                            else if (i < content.Length - 4 && content[(i + 1)..(i + 4)].IsInteger())
                            {
                                var octalAsChar = content[(i + 1)..(i + 4)].ToCharFromOctal();

                                content = content[..i] + octalAsChar + content[(i + 4)..];
                            }
                            // - represent a 2 digit octal character code \53 (equivalent to \053)
                            else if (i < content.Length - 3 && content[(i + 1)..(i + 3)].IsInteger())
                            {
                                var octalAsChar = content[(i + 1)..(i + 3)].ToCharFromOctal();

                                content = content[..i] + octalAsChar + content[(i + 3)..];
                            }
                            // - a single slash which is not part of an escape sequence is ignored
                            else
                            {
                                // Remove the slash
                                content = content.Remove(i, 1);
                            }
                            break;
                    }

                    if (countStart > 0 && countEnd == countStart)
                    {
                        stringEnd = i;
                        stream.Position = stringStart + i;

                        await stream.AdvanceBeyondNextAsync(Constants.RightParenthesis);
                        break;
                    }
                }
            }

            return content[..stringEnd];
        }
    }
}
