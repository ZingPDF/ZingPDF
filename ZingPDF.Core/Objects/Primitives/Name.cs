using System.Text;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    public class Name : PdfObject
    {
        private readonly string _value;

        public Name(string value) : base(value.Length)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"'{nameof(value)}' cannot be null or whitespace.", nameof(value));

            _value = value;
        }

        public override async Task WriteOutputAsync(Stream stream)
        {
            var sb = new StringBuilder();

            foreach (var c in _value)
            {
                if (Constants.Delimiters.Contains(c) || c < 33 || c > 126)
                {
                    sb.Append('#').Append(Convert.ToByte(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            await stream.WriteCharsAsync(Constants.Solidus);
            await stream.WriteTextAsync(sb.ToString());
        }

        public override bool Equals(object? obj)
        {
            return obj is Name name &&
                   _value == name._value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_value);
        }

        public static implicit operator Name(string value) => new(value);
    }
}
