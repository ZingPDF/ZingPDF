using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects
{
    /// <summary>
    /// Represents special PDF keywords, such as 'trailer', or 'startxref'.
    /// </summary>
    internal class PdfKeyword : PdfObject
    {
        public PdfKeyword(string value)
        {
            Value = value;
        }

        public string Value { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync(Value);
            await stream.WriteNewLineAsync();
        }
    }
}
