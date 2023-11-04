using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// Represents special PDF keywords, such as 'trailer', or 'startxref'.
    /// </summary>
    internal class Keyword : PdfObject
    {
        public Keyword(string value)
        {
            Value = value;
        }

        public string Value { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync(Value);
        }

        public static implicit operator Keyword(string value) => new(value);
        public static implicit operator string(Keyword value) => value.Value;
    }
}
