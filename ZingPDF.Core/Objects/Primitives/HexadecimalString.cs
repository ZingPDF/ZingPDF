using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.4.3 - Hexadecimal strings
    /// </summary>
    internal class HexadecimalString : PdfObject
    {
        private HexadecimalString()
        {
            Value = string.Empty;
        }

        public string Value { get; private set; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.LessThan);
            await stream.WriteTextAsync(Value);
            await stream.WriteCharsAsync(Constants.GreaterThan);
        }

        public static HexadecimalString FromBytes(byte[] value) => new() { Value = Convert.ToHexString(value) };
        public static HexadecimalString FromHexStringValue(string value) => new() { Value = value };

        public static implicit operator HexadecimalString(string value) => FromHexStringValue(value);
        public static implicit operator string(HexadecimalString value) => value.Value;
    }
}
