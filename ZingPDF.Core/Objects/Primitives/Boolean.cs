using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.2 - Boolean objects
    /// </summary>
    internal class Boolean : PdfObject
    {
        private readonly bool _value;

        public Boolean(bool value)
        {
            _value = value;
        }

        protected override async Task WriteOutputAsync(Stream stream) => await stream.WriteTextAsync(_value.ToString().ToLower());
    }
}
