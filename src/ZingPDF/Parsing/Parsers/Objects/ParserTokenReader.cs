using System.Buffers;
using System.Text;
using ZingPDF.Extensions;

namespace ZingPDF.Parsing.Parsers.Objects;

internal static class ParserTokenReader
{
    private static readonly Encoding Ascii = Encoding.ASCII;
    private const int DefaultTokenCapacity = 32;
    private const int NumberTokenCapacity = 24;
    private const int ScanBufferSize = 64;
    private static readonly SearchValues<byte> DelimiterOrWhitespace = SearchValues.Create(
    [
        (byte)'(',
        (byte)')',
        (byte)'<',
        (byte)'>',
        (byte)'[',
        (byte)']',
        (byte)'{',
        (byte)'}',
        (byte)'/',
        (byte)'%',
        0x00,
        0x09,
        0x0A,
        0x0C,
        0x0D,
        0x20
    ]);

    public static string ReadName(Stream stream)
    {
        AdvancePastLeadingSolidus(stream);
        return ReadDelimitedToken(stream, DefaultTokenCapacity);
    }

    public static string ReadNumber(Stream stream)
    {
        AdvancePastIgnoredContent(stream);
        return ReadDelimitedToken(stream, NumberTokenCapacity);
    }

    public static string ReadKeyword(Stream stream)
    {
        AdvancePastIgnoredContent(stream);
        return ReadDelimitedToken(stream, DefaultTokenCapacity);
    }

    private static string ReadDelimitedToken(Stream stream, int initialCapacity)
    {
        Span<byte> scanBuffer = stackalloc byte[ScanBufferSize];
        Span<byte> initialBuffer = stackalloc byte[DefaultTokenCapacity];
        byte[]? rented = null;
        var tokenBuffer = initialBuffer[..initialCapacity];
        int count = 0;

        try
        {
            while (true)
            {
                var read = stream.Read(scanBuffer);
                if (read <= 0)
                {
                    break;
                }

                var chunk = scanBuffer[..read];
                var terminatorIndex = chunk.IndexOfAny(DelimiterOrWhitespace);
                var bytesToAppend = terminatorIndex >= 0
                    ? chunk[..terminatorIndex]
                    : chunk;

                EnsureTokenCapacity(ref rented, ref tokenBuffer, count, count + bytesToAppend.Length);
                bytesToAppend.CopyTo(tokenBuffer[count..]);
                count += bytesToAppend.Length;

                if (terminatorIndex >= 0)
                {
                    stream.Position -= read - terminatorIndex;
                    break;
                }
            }

            return Ascii.GetString(tokenBuffer[..count]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static void EnsureTokenCapacity(ref byte[]? rented, ref Span<byte> tokenBuffer, int currentLength, int requiredCapacity)
    {
        if (requiredCapacity <= tokenBuffer.Length)
        {
            return;
        }

        var expanded = ArrayPool<byte>.Shared.Rent(Math.Max(requiredCapacity, tokenBuffer.Length * 2));
        tokenBuffer[..currentLength].CopyTo(expanded);

        if (rented is not null)
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        rented = expanded;
        tokenBuffer = expanded;
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

}
