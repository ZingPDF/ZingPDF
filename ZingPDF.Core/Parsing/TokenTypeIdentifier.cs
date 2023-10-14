using System.Text;
using System.Text.RegularExpressions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    internal static class TokenTypeIdentifier
    {
        private static readonly int _bufferSize = 1024;

        private static readonly Regex _numberPattern = new("^\\d+\\s*");
        private static readonly Regex _namePattern = new(@"^\/.*|\#[\d]+");
        private static readonly Regex _iorPattern = new(@"^[\d]+ [\d]+ R");

        public static async Task<Type?> TryIdentifyAsync(Stream stream)
        {
            var buffer = new byte[_bufferSize];

            var read = await stream.ReadAsync(buffer.AsMemory(0, _bufferSize));

            var content = Encoding.UTF8.GetString(buffer, 0, read).TrimStart();

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            stream.Position -= read;

            if (_namePattern.IsMatch(content))
            {
                return typeof(Name);
            }

            if (_iorPattern.IsMatch(content))
            {
                return typeof(IndirectObjectReference);
            }

            if (_numberPattern.IsMatch(content))
            {
                return typeof(Integer);
            }

            if (content.StartsWith(Constants.DictionaryStart))
            {
                return typeof(Dictionary);
            }

            // This check must always come after the DictionaryStart check.
            if (content.StartsWith(Constants.LessThan))
            {
                return typeof(HexadecimalString);
            }

            if (content.StartsWith(Constants.ArrayStart))
            {
                return typeof(Objects.Primitives.Array);
            }

            if (content.StartsWith(Constants.Trailer))
            {
                return typeof(Trailer);
            }

            throw new ParserException("Unable to identify token from stream");
        }
    }
}
