using System.Text;
using ZingPDF.Syntax.Objects;

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

        public byte[] Decode(byte[] data)
        {
            if (data is null) throw new FilterInputFormatException(nameof(data));
            if (data.Last() != _endOfDataMarker) throw new FilterInputFormatException(nameof(data), $"'{nameof(data)}' must end with the EOD marker: {_endOfDataMarker}.");

            data = data[..^1];

            // Pad odd length data using a zero as per spec.
            if (data.Length % 2 != 0)
            {
                // 48 is the ASCII representation of the character '0'
                data = data.Append((byte)48).ToArray();
            }

            // Create a byte array to store the decoded data
            var decodedData = new List<byte>();

            for (int i = 0; i < data.Length; i += 2)
            {
                // Whitepace characters in input data are to be ignored
                // Read a single byte and check if it's a whitespace character.
                var isWhitespace = Constants.WhitespaceCharacters.Contains(Encoding.ASCII.GetString(data, i, 1)[0]);
                if (isWhitespace)
                {
                    i--;
                    continue;
                }

                var decodedChar = byte.Parse(Encoding.ASCII.GetString(data, i, 2), System.Globalization.NumberStyles.HexNumber);

                decodedData.Add(decodedChar);
            }

            return decodedData.ToArray();
        }

        public byte[] Encode(byte[] data)
        {
            if (data is null) throw new FilterInputFormatException(nameof(data));

            return Encoding.ASCII.GetBytes(Convert.ToHexString(data))
                .Append((byte)_endOfDataMarker)
                .ToArray();
        }
    }
}
