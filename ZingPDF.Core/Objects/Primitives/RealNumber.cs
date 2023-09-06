using System.Globalization;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    internal class RealNumber : PdfObject
    {
        private readonly double _value;

        public RealNumber(double value)
        {
            _value = value;
        }

        public override async Task WriteOutputAsync(Stream stream) => await stream.WriteTextAsync(_value.ToString("G", CultureInfo.InvariantCulture));
    }
}
