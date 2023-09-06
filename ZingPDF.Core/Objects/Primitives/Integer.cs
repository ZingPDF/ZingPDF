using System.Globalization;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    internal class Integer : PdfObject
    {
        private readonly int _value;

        public Integer(int value)
        {
            _value = value;
        }

        public override async Task WriteOutputAsync(Stream stream) => await stream.WriteTextAsync(_value.ToString("G", CultureInfo.InvariantCulture));
    }
}
