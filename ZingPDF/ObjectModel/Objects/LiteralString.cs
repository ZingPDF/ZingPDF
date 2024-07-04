using System.ComponentModel;
using System.Text;
using ZingPDF.Extensions;

namespace ZingPDF.ObjectModel.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.4.2 - Literal strings
    /// </summary>
    public class LiteralString : PdfObject
    {
        private readonly Encoding _encodeUsing;

        public LiteralString(string value, LiteralStringEncoding encodeUsing = LiteralStringEncoding.PDFDocEncoding)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));

            if (!Enum.IsDefined(encodeUsing))
                throw new InvalidEnumArgumentException(nameof(encodeUsing), (int)encodeUsing, typeof(LiteralStringEncoding));

            _encodeUsing = GetEncoding(encodeUsing);
        }

        public string Value { get; private set; }

        public byte[] GetEncodingPreamble() => _encodeUsing.GetPreamble();

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.LeftParenthesis);

            // Write byte order mark
            await stream.WriteAsync(GetEncodingPreamble().AsMemory());

            // TODO: use octals to escape values outside of the specified encoding?
            await stream.WriteTextAsync(Value, _encodeUsing);

            await stream.WriteCharsAsync(Constants.RightParenthesis);
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
                LiteralStringEncoding.PDFDocEncoding => Encoding.Latin1,
                _ => throw new InvalidOperationException(),
            };
        }
    }
}
