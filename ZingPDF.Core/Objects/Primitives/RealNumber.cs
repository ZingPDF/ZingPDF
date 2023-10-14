using System.Globalization;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.3 - Numeric objects
    /// </summary>
    internal class RealNumber : PdfObject
    {
        private readonly double _value;

        public RealNumber(double value)
        {
            _value = value;
        }

        protected override async Task WriteOutputAsync(Stream stream) => await stream.WriteTextAsync(_value.ToString("G", CultureInfo.InvariantCulture));
    }
}
