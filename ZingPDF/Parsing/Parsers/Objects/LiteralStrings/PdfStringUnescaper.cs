using System.Text;

namespace ZingPDF.Parsing.Parsers.Objects.LiteralStrings
{
    /// <summary>
    /// Unescapes PDF string payload bytes:
    /// - Backslash escapes (\\n, \\r, \\t, \\b, \\f, \\\\, \\\\(, \\\\))
    /// - Octal \\ddd (1–3 digits)
    /// - Any other backslash + char → char
    /// </summary>
    internal static class PdfStringUnescaper
    {
        public static byte[] Unescape(byte[] input)
        {
            var output = new List<byte>(input.Length);
            int i = 0;

            while (i < input.Length)
            {
                byte b = input[i];
                if (b == (byte)'\\' && i + 1 < input.Length)
                {
                    byte next = input[i + 1];

                    // Octal escape? 1–3 digits
                    if (next >= (byte)'0' && next <= (byte)'7')
                    {
                        int j = i + 1;
                        int max = Math.Min(input.Length, i + 4);
                        while (j < max && input[j] >= (byte)'0' && input[j] <= (byte)'7')
                            j++;

                        int len = j - (i + 1);
                        string oct = Encoding.ASCII.GetString(input, i + 1, len);
                        byte val = Convert.ToByte(oct, 8);
                        output.Add(val);
                        i += 1 + len;
                        continue;
                    }

                    // Standard escapes
                    switch ((char)next)
                    {
                        case 'n': output.Add((byte)'\n'); break;
                        case 'r': output.Add((byte)'\r'); break;
                        case 't': output.Add((byte)'\t'); break;
                        case 'b': output.Add((byte)'\b'); break;
                        case 'f': output.Add((byte)'\f'); break;
                        case '\\': output.Add((byte)'\\'); break;
                        case '(': output.Add((byte)'('); break;
                        case ')': output.Add((byte)')'); break;
                        default:
                            // backslash before anything else: ignore slash, keep char
                            output.Add(next);
                            break;
                    }
                    i += 2;
                }
                else
                {
                    output.Add(b);
                    i++;
                }
            }

            return [.. output];
        }
    }
}
