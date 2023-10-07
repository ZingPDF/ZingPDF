using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    internal class String : PdfObject
    {
        private readonly string _value;

        public String(string value)
        {
            _value = value;
        }

        public override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteCharsAsync(Constants.StringStart);

            // TODO: handle escaping?
            await stream.WriteTextAsync(_value);

            await stream.WriteCharsAsync(Constants.StringEnd);
        }
    }
}
