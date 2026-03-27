using System.Buffers;
using System.Text;
using ZingPDF.Extensions;

namespace ZingPDF.Parsing.Parsers.Objects;

internal static class ParserTokenReader
{
    private static readonly Encoding Ascii = Encoding.ASCII;

    public static string ReadName(Stream stream)
    {
        AdvancePastLeadingSolidus(stream);
        return ReadToken(stream, IsNameTerminator);
    }

    public static string ReadNumber(Stream stream)
    {
        AdvancePastIgnoredContent(stream);
        return ReadToken(stream, IsDelimiterOrWhitespace);
    }

    public static string ReadKeyword(Stream stream)
    {
        AdvancePastIgnoredContent(stream);
        return ReadToken(stream, IsDelimiterOrWhitespace);
    }

    private static string ReadToken(Stream stream, Func<byte, bool> terminator)
    {
        byte[] rented = ArrayPool<byte>.Shared.Rent(32);
        int count = 0;

        try
        {
            while (stream.Position < stream.Length)
            {
                int next = stream.ReadByte();
                if (next < 0)
                {
                    break;
                }

                byte current = (byte)next;
                if (terminator(current))
                {
                    stream.Position--;
                    break;
                }

                if (count == rented.Length)
                {
                    byte[] expanded = ArrayPool<byte>.Shared.Rent(rented.Length * 2);
                    Buffer.BlockCopy(rented, 0, expanded, 0, count);
                    ArrayPool<byte>.Shared.Return(rented);
                    rented = expanded;
                }

                rented[count++] = current;
            }

            return Ascii.GetString(rented, 0, count);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static void AdvancePastLeadingSolidus(Stream stream)
    {
        while (stream.Position < stream.Length)
        {
            int next = stream.ReadByte();
            if (next < 0)
            {
                return;
            }

            if ((byte)next == (byte)Constants.Characters.Solidus)
            {
                return;
            }
        }
    }

    private static void AdvancePastIgnoredContent(Stream stream)
    {
        while (stream.Position < stream.Length)
        {
            stream.AdvancePastWhitepace();

            if (stream.Position >= stream.Length)
            {
                return;
            }

            int next = stream.ReadByte();
            if (next < 0)
            {
                return;
            }

            if ((byte)next != (byte)Constants.Characters.Percent)
            {
                stream.Position--;
                return;
            }

            while (stream.Position < stream.Length)
            {
                int current = stream.ReadByte();
                if (current < 0 || current is 0x0A or 0x0D)
                {
                    break;
                }
            }
        }
    }

    private static bool IsNameTerminator(byte value)
        => IsDelimiterOrWhitespace(value);

    private static bool IsDelimiterOrWhitespace(byte value)
        => value is
            (byte)'(' or (byte)')' or (byte)'<' or (byte)'>' or
            (byte)'[' or (byte)']' or (byte)'{' or (byte)'}' or
            (byte)'/' or (byte)'%' or
            0x00 or 0x09 or 0x0A or 0x0C or 0x0D or 0x20;
}
