using System.Text;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects.Strings
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.4.2 - Literal strings
    /// </summary>
    public class LiteralString : PdfObject
    {
        private LiteralString(byte[] rawBytes, ObjectOrigin origin)
            : base(origin)
        {
            ArgumentNullException.ThrowIfNull(rawBytes, nameof(rawBytes));

            RawBytes = rawBytes;
        }

        public byte[] RawBytes { get; }

        public override object Clone() => new LiteralString([.. RawBytes], Origin);

        public static LiteralString FromString(string? value, ObjectOrigin objectOrigin)
            => new(value == null ? [] : Encoding.ASCII.GetBytes(value), objectOrigin);

        public static LiteralString FromBytes(byte[] rawBytes, ObjectOrigin objectOrigin)
            => new(rawBytes, objectOrigin);

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.Characters.LeftParenthesis);

            await stream.WriteAsync(RawBytes.ToArray());

            await stream.WriteCharsAsync(Constants.Characters.RightParenthesis);
        }

        public static implicit operator LiteralString(string? value) => FromString(value, ObjectOrigin.UserCreated);
        public static implicit operator string?(LiteralString value) => value?.Decode();
    }
}
