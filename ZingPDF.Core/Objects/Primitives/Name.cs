using System.Text;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.5 Name objects
    /// </summary>
    public class Name : PdfObject
    {
        public Name(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"'{nameof(value)}' cannot be null or whitespace.", nameof(value));

            Value = value;
        }

        public string Value { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            var sb = new StringBuilder();

            foreach (var c in Value)
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
                   Value == name.Value;
        }

        public override int GetHashCode() => HashCode.Combine(Value);

        public override string ToString() => $"{nameof(Name)}: /{Value}";

        public static implicit operator Name(string value) => new(value);
        public static implicit operator string(Name value) => value.Value;
    }
}
