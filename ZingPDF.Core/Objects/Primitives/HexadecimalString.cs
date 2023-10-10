using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    internal class HexadecimalString : PdfObject
    {
        private string _value;

        private HexadecimalString()
        {
            _value = string.Empty;
        }

        public override async Task WriteOutputAsync(Stream stream)
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
