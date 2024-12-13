using System.Globalization;
using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.3 - Numeric objects
    /// </summary>
    public class Integer : PdfObject
    {
        public Integer(int value) : this((long)value) { }

        public Integer(long value)
        {
            Value = value;
        }

        public long Value { get; }

        protected override async Task WriteOutputAsync(Stream stream) => await stream.WriteTextAsync(Value.ToString("G", CultureInfo.InvariantCulture));

        public override string ToString() => $"{nameof(Integer)}: {Value}";

        public static implicit operator Integer(int value) => new(value);
        public static implicit operator Integer(long value) => new(value);

        public static implicit operator ushort(Integer value) => (ushort)value.Value;
        public static implicit operator int?(Integer? value) => (int?)value?.Value;
        public static implicit operator int(Integer value) => (int)value.Value;
        public static implicit operator long(Integer value) => value.Value;

        public static implicit operator Index(Integer value) => Convert.ToInt32(value.Value);
    }
}
