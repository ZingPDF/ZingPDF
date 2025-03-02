using System.Globalization;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.3 - Numeric objects
    /// </summary>
    public class Number(double value) : PdfObject
    {
        public double Value { get; } = value;

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync(Value.ToString("0.######", CultureInfo.InvariantCulture));
        }

        public override string ToString() => $"{nameof(Number)}: {Value}";

        public static implicit operator Number(int value) => new(value);
        public static implicit operator Number(long value) => new(value);

        public static implicit operator ushort(Number value) => (ushort)value.Value;
        public static implicit operator int?(Number? value) => (int?)value?.Value;
        public static implicit operator int(Number value) => (int)value.Value;

        public static implicit operator Index(Number value) => Convert.ToInt32(value.Value);

        public static implicit operator Number(double value) => new(value);
        public static implicit operator double(Number value) => value.Value;
        public static implicit operator long(Number value) => (long)value.Value;

        public static Number operator +(Number a, Number b) => a.Value + b.Value;
        public static Number operator -(Number a, Number b) => a.Value - b.Value;
        public static Number operator *(Number a, Number b) => a.Value * b.Value;
        public static Number operator /(Number a, Number b) => a.Value / b.Value;
    }
}
