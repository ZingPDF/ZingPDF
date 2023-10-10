using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    internal static class TokenTypeIdentifier
    {
        private static readonly Regex _numberPattern = new("^\\d+\\s*");
        private static readonly Regex _namePattern = new(@"^\/.*|\#[\d]+");
        private static readonly Regex _iorPattern = new(@"^[\d]+ [\d]+ R");

        public static bool TryIdentify(string token, [MaybeNullWhen(false)] out Type type)
        {
            token = token.TrimStart();

            if (string.IsNullOrWhiteSpace(token))
            {
                type = null;
                return false;
            }

            if (_namePattern.IsMatch(token))
            {
                type = typeof(Name);
                return true;
            }

            if (_iorPattern.IsMatch(token))
            {
                type = typeof(IndirectObjectReference);
                return true;
            }

            if (_numberPattern.IsMatch(token))
            {
                type = typeof(Integer);
                return true;
            }

            if (token.StartsWith(Constants.DictionaryStart))
            {
                type = typeof(Dictionary);
                return true;
            }

            // This check must always come after the DictionaryStart check.
            if (token.StartsWith(Constants.LessThan))
            {
                type = typeof(HexadecimalString);
                return true;
            }

            if (token.StartsWith(Constants.ArrayStart))
            {
                type = typeof(Objects.Primitives.Array);
                return true;
            }

            type = null;
            return false;
        }
    }
}
