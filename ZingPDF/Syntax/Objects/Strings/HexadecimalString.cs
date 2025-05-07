using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects.Strings
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.4.3 - Hexadecimal strings
    /// </summary>
    /// <remarks>
    /// This type represents byte strings, which are written as hexadecimal strings.
    /// </remarks>
    public class HexadecimalString : PdfObject
    {
        private HexadecimalString(IEnumerable<byte> value, ObjectOrigin objectOrigin)
            : base(objectOrigin)
        {
            RawBytes = value;
        }

        public IEnumerable<byte> RawBytes { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.Characters.LessThan);
            await stream.WriteTextAsync(Convert.ToHexString([.. RawBytes]));
            await stream.WriteCharsAsync(Constants.Characters.GreaterThan);
        }

        public static HexadecimalString FromBytes(byte[] value, ObjectOrigin objectOrigin)
            => new(value, objectOrigin);

        public static HexadecimalString FromHexString(string value, ObjectOrigin objectOrigin)
            => new(Convert.FromHexString(value), objectOrigin);

        public override object Clone()
        {
            return new HexadecimalString(RawBytes, Origin);
        }

        public static implicit operator string(HexadecimalString value) => Convert.ToHexString([.. value.RawBytes]);
        public static implicit operator HexadecimalString(string value) => FromHexString(value, ObjectOrigin.ImplicitOperatorConversion);
    }
}
