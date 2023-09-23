using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects.Filters
{
    /// <summary>
    /// ISO 32000-2:2020 7.4.2
    /// 
    /// Represents data using ASCII hexadecimal encoding.
    /// Each byte is represented by two ASCII characters.
    /// </summary>
    internal class ASCIIHexDecodeFilter : IFilter
    {
        public Name Name => "ASCIIHexDecode";
        public string EndOfDataMarker => ">";

        public FilterParams? Params => null;

        public byte[] Decode(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) throw new ArgumentException($"'{nameof(data)}' cannot be null or whitespace.", nameof(data));
            if (!data.EndsWith(EndOfDataMarker)) throw new ArgumentException($"'{nameof(data)}' must end with the EOD marker: {EndOfDataMarker}.", nameof(data));

            // Remove unwanted whitespace characters
            data = string.Join("", data.Split(Constants.WhitespaceCharacters));

            // Remove EOD marker
            data = data[..^1];

            // Pad odd length string with trailing zero
            if (data.Length % 2 == 1)
            {
                data += '0';
            }

            return StringToByteArray(data);
        }

        public string Encode(byte[] data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));

           return Convert.ToHexString(data) + EndOfDataMarker;
        }

        private static byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
            }

            return arr;
        }

        private static int GetHexVal(char hex)
        {
            int val = hex;

            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);

            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);

            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}
