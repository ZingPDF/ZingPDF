using MorseCode.ITask;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Encryption;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers.Objects;

/// <summary>
/// Parses a PDF string object from a stream (syntax only: literal or hex).
/// Does not assign semantic Kind/Encoding (defaults to Byte). Promote later using AsText()/AsAscii().
/// </summary>
internal class PdfStringParser : IParser<PdfString>
{
    private readonly IPdfEncryptionProvider _encryptionProvider;

    public PdfStringParser()
        : this(NoOpPdfEncryptionProvider.Instance)
    {
    }

    public PdfStringParser(IPdfEncryptionProvider encryptionProvider)
    {
        _encryptionProvider = encryptionProvider;
    }

    public async ITask<PdfString> ParseAsync(Stream stream, ObjectContext context)
    {
        // Skip leading whitespace/comments if your infra doesn't already
        int first = ReadNonWs(stream);
        if (first < 0) throw new EndOfStreamException("Unexpected EOF reading string.");

        if (first == '(')
        {
            var bytes = ParseLiteralBytes(stream);
            bytes = await _encryptionProvider.DecryptObjectBytesAsync(context, bytes, null);
            return PdfString.FromBytes(bytes, PdfStringSyntax.Literal, context);
        }

        if (first == '<')
        {
            var bytes = ParseHexBytes(stream);
            bytes = await _encryptionProvider.DecryptObjectBytesAsync(context, bytes, null);
            return PdfString.FromBytes(bytes, PdfStringSyntax.Hex, context);
        }

        throw new ParserException($"Expected string '(' or '<', found '{(char)first}'.");
    }

    // ---- literal "( ... )" with escapes, §7.3.4.2
    private static byte[] ParseLiteralBytes(Stream s)
    {
        var buf = new List<byte>(64);
        int depth = 1; // support balanced parentheses with escapes
        while (true)
        {
            int ch = s.ReadByte();
            if (ch < 0) throw new EndOfStreamException("Unterminated literal string.");

            if (ch == '\\') // escape
            {
                int n = s.ReadByte();
                if (n < 0) throw new EndOfStreamException("Unterminated escape in string.");

                switch (n)
                {
                    // Line continuation: backslash + EOL consumes both, adds nothing
                    case '\r':
                        {
                            int maybeLf = s.PeekByte();
                            if (maybeLf == '\n') s.ReadByte(); // consume LF after CR
                            // nothing emitted
                            continue;
                        }
                    case '\n':
                        continue;

                    case 'n': buf.Add(0x0A); continue;
                    case 'r': buf.Add(0x0D); continue;
                    case 't': buf.Add(0x09); continue;
                    case 'b': buf.Add(0x08); continue;
                    case 'f': buf.Add(0x0C); continue;
                    case '(': buf.Add((byte)'('); continue;
                    case ')': buf.Add((byte)')'); continue;
                    case '\\': buf.Add((byte)'\\'); continue;

                    // Octal: up to 3 digits (0-7), greedy
                    case >= '0' and <= '7':
                        {
                            int v = n - '0';
                            for (int i = 0; i < 2; i++)
                            {
                                int peek = s.PeekByte();
                                if (peek >= '0' && peek <= '7')
                                {
                                    s.ReadByte();
                                    v = (v << 3) + (peek - '0');
                                }
                                else break;
                            }
                            buf.Add((byte)(v & 0xFF));
                            continue;
                        }

                    default:
                        // Any other escaped char just yields itself (spec allows)
                        buf.Add((byte)n);
                        continue;
                }
            }

            if (ch == '(')
            {
                depth++;
                buf.Add((byte)'(');
                continue;
            }

            if (ch == ')')
            {
                depth--;
                if (depth == 0)
                {
                    break; // done; do not include closing ')'
                }

                buf.Add((byte)')');
                continue;
            }

            buf.Add((byte)ch);
        }

        // Return as Byte-kind by default; caller promotes based on context
        return [.. buf];
    }

    // ---- hex "< ... >", §7.3.4.3
    private static byte[] ParseHexBytes(Stream s)
    {
        var nibbles = new List<int>(64);

        while (true)
        {
            int ch = s.ReadByte();
            if (ch < 0) throw new EndOfStreamException("Unterminated hex string.");
            if (IsWhite(ch)) continue;
            if (ch == '>') break;

            int v = HexVal(ch);
            if (v < 0) throw new ParserException($"Invalid hex digit '{(char)ch}' in hex string.");
            nibbles.Add(v);
        }

        if ((nibbles.Count & 1) == 1)
            nibbles.Add(0); // pad low nibble with 0 per spec

        var bytes = new byte[nibbles.Count / 2];
        for (int i = 0, j = 0; i < bytes.Length; i++, j += 2)
            bytes[i] = (byte)(nibbles[j] << 4 | nibbles[j + 1]);

        return bytes;
    }

    // ---- helpers ----

    private static int ReadNonWs(Stream s)
    {
        int b;
        while ((b = s.ReadByte()) >= 0)
        {
            if (!IsWhite(b)) return b;
        }

        return -1;
    }

    private static bool IsWhite(int c) =>
        c is 0x00 or 0x09 or 0x0A or 0x0C or 0x0D or 0x20;

    private static int HexVal(int ch) => ch switch
    {
        >= '0' and <= '9' => ch - '0',
        >= 'A' and <= 'F' => 10 + (ch - 'A'),
        >= 'a' and <= 'f' => 10 + (ch - 'a'),
        _ => -1
    };
}

// Handy Stream peeker (non-alloc, non-async for single byte lookahead)
internal static class StreamExtensions
{
    public static int PeekByte(this Stream s)
    {
        int b = s.ReadByte();

        if (b >= 0)
        {
            s.Position -= 1;
        }

        return b;
    }
}
