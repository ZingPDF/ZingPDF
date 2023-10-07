using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    internal static class TokenTypeIdentifier
    {
        private static readonly Regex _namePattern = new(@"\/.*|\#[\d]+");
        private static readonly Regex _iorPattern = new(@"[\d]+ [\d]+ R");

        public static bool TryIdentify(string token, [MaybeNullWhen(false)] out Type type)
        {
            if (_namePattern.IsMatch(token))
            {
                type = typeof(Name);
                return true;
            }

            if (token.IsInteger())
            {
                type = typeof(Integer);
                return true;
            }

            if (_iorPattern.IsMatch(token))
            {
                type = typeof(IndirectObjectReference);
                return true;
            }

            type = null;
            return false;
        }
    }
}
