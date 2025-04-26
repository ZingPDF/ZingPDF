using System.ComponentModel;
using System.Text;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects.Strings
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.4.2 - Literal strings
    /// </summary>
    /// <remarks>
    /// This type represents 2 of the 3 string types from the spec: text, and ascii.
    /// The text type supports the 3 encoding types: UTF8, UTF16BE, and PDFDocEncoding.
    /// </remarks>
    public class LiteralString : PdfObject
    {
        private readonly Encoding _encodeUsing;

        public LiteralString(byte[] rawBytes, LiteralStringEncoding encodeUsing = LiteralStringEncoding.PDFDocEncoding)
        {
            ArgumentNullException.ThrowIfNull(rawBytes, nameof(rawBytes));

            RawBytes = rawBytes;

            _encodeUsing = GetEncoding(encodeUsing);
        }

        public LiteralString(string value, LiteralStringEncoding encodeUsing = LiteralStringEncoding.PDFDocEncoding)
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));

            if (!Enum.IsDefined(encodeUsing))
                throw new InvalidEnumArgumentException(nameof(encodeUsing), (int)encodeUsing, typeof(LiteralStringEncoding));

            _encodeUsing = GetEncoding(encodeUsing);

            RawBytes = _encodeUsing.GetBytes(value);
        }

        public byte[] RawBytes { get; }
        public string Value => _encodeUsing.GetString(RawBytes);

        public byte[] GetEncodingPreamble() => _encodeUsing.GetPreamble();

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.Characters.LeftParenthesis);

            // Write byte order mark
            await stream.WriteAsync(GetEncodingPreamble().AsMemory());

            // TODO: use octals to escape values outside of the specified encoding?
            await stream.WriteTextAsync(Value, _encodeUsing);

            await stream.WriteCharsAsync(Constants.Characters.RightParenthesis);
        }

        public static implicit operator LiteralString?(string? value) => value is null ? null : new(value);
        public static implicit operator string?(LiteralString? value) => value?.Value;

        public override string ToString() => Value;

        private static Encoding GetEncoding(LiteralStringEncoding encoding)
        {
            return encoding switch
            {
                LiteralStringEncoding.UTF8 => Encoding.UTF8,
                LiteralStringEncoding.UTF16BE => Encoding.BigEndianUnicode,
                LiteralStringEncoding.PDFDocEncoding => Encoding.GetEncoding("PdfDocEncoding"),
                _ => throw new InvalidOperationException(),
            };
        }
    }
}
