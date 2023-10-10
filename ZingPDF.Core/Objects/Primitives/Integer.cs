using System.Globalization;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    internal class Integer : PdfObject
    {
        private readonly long _value;

        public Integer(int value) : this((long)value) { }

        public Integer(long value)
        {
            _value = value;
        }

        public override async Task WriteOutputAsync(Stream stream) => await stream.WriteTextAsync(_value.ToString("G", CultureInfo.InvariantCulture));

        public static implicit operator Integer(int value) => new(value);
        public static implicit operator Integer(long value) => new(value);
        public static implicit operator int(Integer value) => (int)value._value;
        public static implicit operator long(Integer value) => value._value;
    }
}
