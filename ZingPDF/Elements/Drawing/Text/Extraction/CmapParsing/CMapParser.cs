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
            if (line.EndsWith("beginbfchar"))
            {
                int count = int.Parse(line.Split(' ')[0]);
                for (int i = 0; i < count; i++)
                {
                    var parts = reader.ReadLine()?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts?.Length == 2)
                    {
                        var src = HexToBytes(parts[0]);
                        var dst = DecodeUtf16Be(parts[1]);
                        cmap.AddMapping(src, dst);
                    }
                }
            }
            else if (line.EndsWith("beginbfrange"))
            {
                int count = int.Parse(line.Split(' ')[0]);
                for (int i = 0; i < count; i++)
                {
                    var parts = reader.ReadLine()?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts == null || parts.Length < 3) continue;

                    var start = HexToBytes(parts[0]);
                    var end = HexToBytes(parts[1]);

                    if (parts[2].StartsWith("<"))
                    {
                        var dstStart = HexToBytes(parts[2]);
                        int rangeCount = ByteArrayToInt(end) - ByteArrayToInt(start) + 1;

                        for (int j = 0; j < rangeCount; j++)
                        {
                            var src = IntToByteArray(ByteArrayToInt(start) + j, start.Length);
                            var dst = DecodeUtf16Be(dstStart, j);
                            cmap.AddMapping(src, dst);
                        }
                    }
                    else if (parts[2] == "[")
                    {
                        var dsts = new List<string>();
                        string innerLine;
                        while (!(innerLine = reader.ReadLine() ?? "").Contains("]"))
                        {
                            dsts.AddRange(innerLine.Trim().Split(' ').Where(x => x.StartsWith("<")).Select(DecodeUtf16Be));
                        }
                        dsts.AddRange(innerLine.Trim().Split(' ').Where(x => x.StartsWith("<")).Select(DecodeUtf16Be));

                        int startInt = ByteArrayToInt(start);
                        for (int j = 0; j < dsts.Count; j++)
                        {
                            var src = IntToByteArray(startInt + j, start.Length);
                            cmap.AddMapping(src, dsts[j]);
                        }
                    }
                }
            }
        }

        return cmap;
    }

    private static byte[] HexToBytes(string hex)
    {
        hex = hex.Trim('<', '>');
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }

    private static string DecodeUtf16Be(string hex)
    {
        var bytes = HexToBytes(hex);
        return Encoding.BigEndianUnicode.GetString(bytes);
    }

    private static string DecodeUtf16Be(byte[] start, int offset)
    {
        int codeUnitCount = start.Length / 2;
        byte[] bytes = new byte[codeUnitCount * 2];
        Buffer.BlockCopy(start, 0, bytes, 0, bytes.Length);

        // Increment by offset
        for (int i = 0; i < offset; i++)
        {
            bool carry = true;
            for (int j = bytes.Length - 1; j >= 0 && carry; j--)
            {
                carry = ++bytes[j] == 0;
            }
        }

        return Encoding.BigEndianUnicode.GetString(bytes);
    }

    private static int ByteArrayToInt(byte[] bytes)
    {
        int result = 0;
        for (int i = 0; i < bytes.Length; i++)
            result = (result << 8) | bytes[i];
        return result;
    }

    private static byte[] IntToByteArray(int value, int length)
    {
        byte[] result = new byte[length];
        for (int i = length - 1; i >= 0; i--)
        {
            result[i] = (byte)(value & 0xFF);
            value >>= 8;
        }
        return result;
    }
}
