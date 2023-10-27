using System.Globalization;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.3 - Numeric objects
    /// </summary>
    internal class RealNumber : PdfObject
    {
        public RealNumber(double value)
        {
            Value = value;
        }

        public double Value { get; }

        protected override async Task WriteOutputAsync(Stream stream) => await stream.WriteTextAsync(Value.ToString("G", CultureInfo.InvariantCulture));

        public override string ToString() => $"{nameof(Integer)}: {Value}";

        public static implicit operator RealNumber(double value) => new(value);
        public static implicit operator RealNumber(long value) => new(value);
        public static implicit operator RealNumber(int value) => new(value);

        public static implicit operator double(RealNumber value) => value.Value;
        public static implicit operator long(RealNumber value) => (long)value.Value;
        public static implicit operator int(RealNumber value) => (int)value.Value;
    }
}
