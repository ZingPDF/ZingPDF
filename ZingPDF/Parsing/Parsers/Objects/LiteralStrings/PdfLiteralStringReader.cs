namespace ZingPDF.Parsing.Parsers.Objects.LiteralStrings
{
    /// <summary>
    /// Reads the raw bytes of a PDF literal string, handling nested parentheses,
    /// escaped parentheses, and line continuations. Stops after consuming the closing parenthesis.
    /// </summary>
    internal static class PdfLiteralStringReader
    {
        public static byte[] ReadRawLiteral(Stream stream)
        {
            // Expect and consume the opening '('
            int first = stream.ReadByte();
            if (first != Constants.Characters.LeftParenthesis)
                throw new InvalidDataException("Literal string must start with '('.");

            var buffer = new List<byte>();
            int nesting = 1;

            while (nesting > 0)
            {
                int raw = stream.ReadByte();
                if (raw < 0)
                    throw new EndOfStreamException("Unexpected EOF in literal string.");

                byte b = (byte)raw;

                if (b == (byte)'\\')
                {
                    // Peek next byte
                    int next = stream.ReadByte();
                    if (next < 0)
                        throw new EndOfStreamException("Unexpected EOF after escape in literal string.");

                    byte nb = (byte)next;

                    // Line continuation: backslash + EOL (CR, LF, or CRLF)
                    if (nb == (byte)'\r' || nb == (byte)'\n')
                    {
                        // If CRLF, consume LF
                        if (nb == (byte)'\r')
                        {
                            int maybeLf = stream.ReadByte();
                            if (maybeLf != (byte)'\n')
                                stream.Position--; // push back if not LF
                        }
                        // Skip both backslash and EOL
                        continue;
                    }

                    // Otherwise: escaped char; preserve both bytes so Unescaper can process
                    buffer.Add(b);
                    buffer.Add(nb);
                    continue;
                }
                else if (b == (byte)Constants.Characters.LeftParenthesis)
                {
                    nesting++;
                    buffer.Add(b);
                }
                else if (b == (byte)Constants.Characters.RightParenthesis)
                {
                    nesting--;
                    if (nesting > 0)
                        buffer.Add(b);
                    // else: top-level closing, do not include
                }
                else
                {
                    buffer.Add(b);
                }
            }

            return [.. buffer];
        }
    }
}
