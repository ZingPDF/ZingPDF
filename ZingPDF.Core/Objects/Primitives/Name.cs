using System.Text;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    internal class Name : PdfObject
    {
        private readonly string _value;

        public Name(string value)
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

            await stream.WriteTextAsync(Constants.Solidus);
            await stream.WriteTextAsync(sb.ToString());
        }

        public static implicit operator Name(string value) => new(value);
    }
}
