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

        public Dictionary Parse(IEnumerable<string> tokens)
        {
            // trailer
            // << /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>
            // startxref
            // 148076

            var startIndex = tokens.ToList().IndexOf(Constants.DictionaryStart);

            if (startIndex == -1)
            {
                throw new ParserException();
            }

            var dictionary = new Dictionary();

            foreach (var token in tokens.Skip(startIndex + 1))
            {
                // The key is always a name.
                var index = token.IndexOf(Constants.Whitespace);
                Name key = token[1..index];

                // The value can be anything.
                var value = token[(index + 1)..token.Length];

                if (!TokenTypeIdentifier.TryIdentify(value, out var tokenType))
                {
                    throw new ParserException();
                }

                var obj = Parser.For(tokenType).Parse(new[] { value });

                dictionary[key] = obj;
            }

            return dictionary;
        }
    }
}