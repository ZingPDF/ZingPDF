using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    /// <summary>
    /// Creates a <see cref="Dictionary"/> from the provided tokens.
    /// </summary>
    /// <remarks>
    /// This parser will find the first &lt;&lt; delimiter from the provided tokens.
    /// </remarks>
    internal class DictionaryParser : IPdfObjectParser<Dictionary>
    {
        private static string _defaultExceptionMessage = "Invalid dictionary";

        public Dictionary Parse(string content)
        {
            // trailer
            // << /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>
            // startxref
            // 148076

            var dictionary = new Dictionary();

            var startIndex = content.IndexOf(Constants.DictionaryStart);
            if (startIndex == -1)
            {
                throw new ParserException();
            }

            var nameParser = Parser.For<Name>();

            var keyIndex = content.IndexOf(Constants.Solidus, startIndex);

            do
            {
                // The key is always a name.
                var key = nameParser.Parse(content);

                // The value can be anything.
                var valueStartIndex = keyIndex + (int)key.Length!;
                var valueContent = content[valueStartIndex..];

                if (!TokenTypeIdentifier.TryIdentify(valueContent, out var tokenType))
                {
                    throw new ParserException();
                }

                var obj = Parser.For(tokenType).Parse(valueContent);

                dictionary[key] = obj;

                if (obj.Length.HasValue)
                {
                    startIndex += (int)obj.Length;
                }
                else
                {
                    // TODO: where to start next when we don't have a length?
                }

                keyIndex = content.IndexOf(Constants.Solidus, startIndex);
            }
            while (keyIndex != -1);

            return dictionary;
        }
    }
}