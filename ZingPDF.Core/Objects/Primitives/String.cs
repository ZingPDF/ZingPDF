using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.4.2 - Literal strings
    /// </summary>
    internal class String : PdfObject
    {
        private readonly string _value;

        public String(string value)
        {
            _value = value;
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.StringStart);

            // TODO: handle escaping?
            await stream.WriteTextAsync(_value);

            await stream.WriteCharsAsync(Constants.StringEnd);
        }
    }
}
