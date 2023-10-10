using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects
{
    /// <summary>
    /// Represents special PDF keywords, such as 'trailer', or 'startxref'.
    /// </summary>
    internal class PdfKeyword : PdfObject
    {
        public PdfKeyword(string value) : base(value.Length)
        {
            Value = value;
        }

        public string Value { get; }

        public override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync(Value);
            await stream.WriteNewLineAsync();
        }
    }
}
