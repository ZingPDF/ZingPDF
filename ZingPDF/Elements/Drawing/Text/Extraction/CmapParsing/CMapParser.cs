using System.Text;

namespace ZingPDF.Elements.Drawing.Text.Extraction.CmapParsing;

public class CMapParser
{
    public static CMap Parse(Stream stream)
    {
        var cmap = new CMap();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.EndsWith("begincodespacerange"))
            {
                int count = int.Parse(line.Split(' ')[0]);
                for (int i = 0; i < count; i++)
                {
                    var parts = reader.ReadLine()?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts?.Length == 2)
                    {
                        cmap.RegisterCodeLength(GetHexByteLength(parts[0]));
                        cmap.RegisterCodeLength(GetHexByteLength(parts[1]));
                    }
                }
            }
            else if (line.EndsWith("beginbfchar"))
            {
                int count = int.Parse(line.Split(' ')[0]);
                for (int i = 0; i < count; i++)
                {
                    var parts = reader.ReadLine()?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts?.Length == 2)
                    {
                        cmap.AddMapping(HexToBytes(parts[0]), DecodeUtf16Be(parts[1]));
                    }
                }
            }
            else if (line.EndsWith("beginbfrange"))
            {
                int count = int.Parse(line.Split(' ')[0]);
                for (int i = 0; i < count; i++)
                {
                    var parts = reader.ReadLine()?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts == null || parts.Length < 3)
                    {
                        continue;
                    }

                    var start = HexToBytes(parts[0]);
                    var startValue = ByteArrayToUInt64(start);
                    var endValue = HexToUInt64(parts[1]);

                    if (parts[2].StartsWith("<", StringComparison.Ordinal))
                    {
                        var dstStartValue = HexToUInt64(parts[2]);
                        var dstByteLength = GetHexByteLength(parts[2]);
                        var rangeCount = checked((int)(endValue - startValue + 1));

                        for (var j = 0; j < rangeCount; j++)
                        {
                            cmap.AddMapping(startValue + (uint)j, start.Length, DecodeUtf16Be(dstStartValue + (uint)j, dstByteLength));
                        }
                    }
                    else if (parts[2] == "[")
                    {
                        var sourceValue = startValue;
                        string? innerLine;

                        while ((innerLine = reader.ReadLine()) != null)
                        {
                            var index = 0;
                            var innerSpan = innerLine.AsSpan();
                            while (TryReadNextHexToken(innerSpan, ref index, out var token))
                            {
                                cmap.AddMapping(sourceValue, start.Length, DecodeUtf16Be(token));
                                sourceValue++;
                            }

                            if (innerSpan.IndexOf(']') >= 0)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        return cmap;
    }

    private static byte[] HexToBytes(string hex)
    {
        var digits = GetHexDigits(hex.AsSpan());
        byte[] bytes = new byte[digits.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)((HexValue(digits[i * 2]) << 4) | HexValue(digits[(i * 2) + 1]));
        }

        return bytes;
    }

    private static string DecodeUtf16Be(string hex) => DecodeUtf16Be(GetHexDigits(hex.AsSpan()));

    private static string DecodeUtf16Be(ReadOnlySpan<char> hexDigits)
    {
        var digits = GetHexDigits(hexDigits);
        var byteLength = digits.Length / 2;
        var value = HexToUInt64(digits);
        return DecodeUtf16Be(value, byteLength);
    }

    private static string DecodeUtf16Be(ulong value, int byteLength)
    {
        var codeUnitCount = byteLength / 2;
        Span<char> chars = codeUnitCount <= 4 ? stackalloc char[codeUnitCount] : new char[codeUnitCount];

        for (var i = codeUnitCount - 1; i >= 0; i--)
        {
            chars[i] = (char)(value & 0xFFFF);
            value >>= 16;
        }

        return new string(chars);
    }

    private static ulong ByteArrayToUInt64(byte[] bytes)
    {
        ulong result = 0;
        for (var i = 0; i < bytes.Length; i++)
        {
            result = (result << 8) | bytes[i];
        }

        return result;
    }

    private static ulong HexToUInt64(string token) => HexToUInt64(GetHexDigits(token.AsSpan()));

    private static ulong HexToUInt64(ReadOnlySpan<char> hexDigits)
    {
        ulong result = 0;
        for (var i = 0; i < hexDigits.Length; i++)
        {
            result = (result << 4) | (uint)HexValue(hexDigits[i]);
        }

        return result;
    }

    private static ReadOnlySpan<char> GetHexDigits(ReadOnlySpan<char> token)
    {
        token = token.Trim();
        if (token.Length >= 2 && token[0] == '<' && token[^1] == '>')
        {
            return token[1..^1];
        }

        return token;
    }

    private static int GetHexByteLength(string token) => GetHexDigits(token.AsSpan()).Length / 2;

    private static bool TryReadNextHexToken(ReadOnlySpan<char> line, ref int index, out ReadOnlySpan<char> token)
    {
        while (index < line.Length && char.IsWhiteSpace(line[index]))
        {
            index++;
        }

        if (index >= line.Length || line[index] != '<')
        {
            token = default;
            return false;
        }

        var start = index;
        index++;
        while (index < line.Length && line[index] != '>')
        {
            index++;
        }

        if (index >= line.Length)
        {
            token = default;
            return false;
        }

        index++;
        token = line[start..index];
        return true;
    }

    private static int HexValue(char value) => value switch
    {
        >= '0' and <= '9' => value - '0',
        >= 'A' and <= 'F' => 10 + (value - 'A'),
        >= 'a' and <= 'f' => 10 + (value - 'a'),
        _ => throw new FormatException($"Invalid hex character '{value}'.")
    };
}
