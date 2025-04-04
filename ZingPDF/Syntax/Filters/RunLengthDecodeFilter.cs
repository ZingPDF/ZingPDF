using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Filters;

internal class RunLengthDecodeFilter : IFilter
{
    public Name Name => Constants.Filters.RunLength;
    public Dictionary? Params => null;

    public MemoryStream Decode(Stream data)
    {
        var output = new MemoryStream();
        Span<byte> buffer = stackalloc byte[128]; // Max literal run size

        while (true)
        {
            int lengthByte = data.ReadByte();
            if (lengthByte == -1)
                throw new FilterInputFormatException(nameof(data), "Unexpected end of stream.");
            if (lengthByte == 128) break; // EOD

            if (lengthByte < 128)
            {
                int runLength = lengthByte + 1;
                int read = data.Read(buffer[..runLength]);
                if (read != runLength)
                    throw new FilterInputFormatException(nameof(data), "Truncated literal run.");
                output.Write(buffer[..runLength]);
            }
            else // lengthByte > 128
            {
                int runLength = 257 - lengthByte;
                int value = data.ReadByte();
                if (value == -1)
                    throw new FilterInputFormatException(nameof(data), "Truncated repeat run.");
                output.WriteByte((byte)value);
                for (int i = 1; i < runLength; i++)
                    output.WriteByte((byte)value);
            }
        }

        output.Position = 0;
        return output;
    }

    public MemoryStream Encode(Stream data)
    {
        if (data is null) throw new FilterInputFormatException(nameof(data));

        using var inputBuffer = new MemoryStream();
        data.CopyTo(inputBuffer);
        byte[] input = inputBuffer.ToArray();

        var output = new MemoryStream();
        int i = 0;

        while (i < input.Length)
        {
            byte currentByte = input[i];
            int runLength = 1;

            // Check for repeating run
            while (i + runLength < input.Length && input[i + runLength] == currentByte && runLength < 128)
            {
                runLength++;
            }

            if (runLength > 1)
            {
                // Emit repeated run
                output.WriteByte((byte)(257 - runLength));
                output.WriteByte(currentByte);
            }
            else
            {
                // Literal run
                int start = i;
                runLength = 1;
                while (
                    start + runLength < input.Length &&
                    runLength < 128 &&
                    (start + runLength == input.Length - 1 || input[start + runLength] != input[start + runLength + 1])
                )
                {
                    runLength++;
                }

                output.WriteByte((byte)(runLength - 1));
                output.Write(input, start, runLength);
            }

            i += runLength;
        }

        // End of Data marker
        output.WriteByte(128);
        output.Position = 0;
        return output;
    }
}
