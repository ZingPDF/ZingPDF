using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.4.3 - Hexadecimal strings
    /// </summary>
    internal class HexadecimalString : PdfObject
    {
        private string _value;

        private HexadecimalString()
        {
            _value = string.Empty;
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.LessThan);
            await stream.WriteTextAsync(_value);
            await stream.WriteCharsAsync(Constants.GreaterThan);
        }

        public static HexadecimalString FromBytes(byte[] value) => new() { _value = Convert.ToHexString(value) };
        public static HexadecimalString FromHexStringValue(string value) => new() { _value = value };

        public static implicit operator HexadecimalString(string value) => FromHexStringValue(value);
    }
}
