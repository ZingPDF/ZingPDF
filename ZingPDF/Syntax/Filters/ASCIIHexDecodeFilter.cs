using System.Text;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Filters
{
    /// <summary>
    /// ISO 32000-2:2020 7.4.2
    /// 
    /// ASCIIHexDecode
    /// 
    /// Represents data using ASCII hexadecimal encoding.
    /// Each byte is represented by two ASCII characters.
    /// </summary>
    internal class ASCIIHexDecodeFilter : IFilter
    {
        private readonly int _endOfDataMarker = '>';

        public Name Name => Constants.Filters.ASCIIHex;
        public Dictionary? Params => null;

        public MemoryStream Decode(Stream data)
        {
            if (data is null) throw new FilterInputFormatException(nameof(data));

            using var inputBuffer = new MemoryStream(); // temporary buffer to inspect last byte
            data.CopyTo(inputBuffer);

            if (inputBuffer.Length == 0)
                throw new FilterInputFormatException(nameof(data), "Input stream is empty.");

            var buffer = inputBuffer.GetBuffer();
            var length = (int)inputBuffer.Length;

            if (buffer[length - 1] != _endOfDataMarker)
                throw new FilterInputFormatException(nameof(data), $"'{nameof(data)}' must end with the EOD marker: {_endOfDataMarker}.");

            var output = new MemoryStream(capacity: length / 2); // rough initial capacity guess
            Span<byte> hexPair = stackalloc byte[2];

            int i = 0;
            int end = length - 1; // exclude EOD marker

            while (i < end)
            {
                // Skip whitespace characters
                if (Constants.WhitespaceCharacters.Contains((char)buffer[i]))
                {
                    i++;
                    continue;
                }

                // Read first hex digit
                hexPair[0] = buffer[i++];
                while (i < end && Constants.WhitespaceCharacters.Contains((char)buffer[i]))
                    i++;

                if (i >= end)
                {
                    // Pad if odd number of digits
                    hexPair[1] = (byte)'0';
                }
                else
                {
                    hexPair[1] = buffer[i++];
                }

                var hexStr = Encoding.ASCII.GetString(hexPair);
                var b = byte.Parse(hexStr, System.Globalization.NumberStyles.HexNumber);
                output.WriteByte(b);
            }

            output.Position = 0;
            return output;
        }

        public MemoryStream Encode(Stream data)
        {
            if (data is null) throw new FilterInputFormatException(nameof(data));

            var output = new MemoryStream();

            Span<byte> hex = stackalloc byte[2];
            int b;

            while ((b = data.ReadByte()) != -1)
            {
                // Convert each byte to its two-character uppercase hex representation
                byte upper = (byte)ToHexChar((b >> 4) & 0xF);
                byte lower = (byte)ToHexChar(b & 0xF);
                hex[0] = upper;
                hex[1] = lower;
                output.Write(hex);
            }

            output.WriteByte((byte)_endOfDataMarker);
            output.Position = 0;
            return output;

            static char ToHexChar(int value)
                => (char)(value < 10 ? '0' + value : 'A' + (value - 10));
        }
    }
}
