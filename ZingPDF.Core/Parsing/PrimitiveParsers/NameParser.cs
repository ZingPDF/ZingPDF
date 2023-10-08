using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class NameParser : IPdfObjectParser<Name>
    {
        public Name Parse(string content)
        {
            var keyStartIndex = content.IndexOf(Constants.Solidus) + 1;
            var keyEndIndex = content.IndexOfAny(new[] { Constants.Solidus, Constants.Space }, keyStartIndex);

            if (keyEndIndex == -1)
            {
                keyEndIndex = content.Length;
            }

            return content[keyStartIndex..keyEndIndex];
        }
    }
}
