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
        private HexadecimalString(IEnumerable<byte> value, ObjectContext context)
            : base(context)
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

        public static HexadecimalString FromBytes(byte[] value, ObjectContext context)
            => new(value, context);

        public static HexadecimalString FromHexString(string value, ObjectContext context)
            => new(Convert.FromHexString(value), context);

        public override object Clone()
        {
            return new HexadecimalString(RawBytes, Context);
        }

        public static implicit operator string(HexadecimalString value) => Convert.ToHexString([.. value.RawBytes]);
        public static implicit operator HexadecimalString(string value) => FromHexString(value, ObjectContext.FromImplicitOperator);
    }
}
