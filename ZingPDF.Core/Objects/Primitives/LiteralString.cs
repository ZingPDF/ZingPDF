using System.Text;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.4.2 - Literal strings
    /// </summary>
    internal class LiteralString : PdfObject
    {
        private readonly Encoding _encodeUsing;

        public LiteralString(string value, Encoding? encodeUsing = null)
        {
            Value = value;

            _encodeUsing = encodeUsing ?? Encoding.UTF8;

            if (
                _encodeUsing is not UTF8Encoding
                && _encodeUsing is not PDFDocEncoding
                && (_encodeUsing is not UnicodeEncoding || _encodeUsing is UnicodeEncoding u && u.CodePage != 1201)
                )
            {
                throw new ArgumentException("Invalid Encoding specified", nameof(encodeUsing));
            }
        }

        public string Value { get; private set; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.LeftParenthesis);

            await stream.WriteTextAsync(Value, _encodeUsing);

            await stream.WriteCharsAsync(Constants.RightParenthesis);
        }

        public static implicit operator LiteralString(string value) => new(value);
        public static implicit operator string(LiteralString value) => value.Value;
    }
}
