using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    internal class Boolean : PdfObject
    {
        private readonly bool _value;

        public Boolean(bool value)
        {
            _value = value;
        }

        public override async Task WriteOutputAsync(Stream stream) => await stream.WriteTextAsync(_value.ToString().ToLower());
    }
}
