using ZingPdf.Core.Objects;
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

        public IParseResult<Dictionary> Parse(string content)
        {
            // A dictionary is a key-value collection, where the key is always a 'Name' object
            // and the valuie can be any type of PDF object

            // << /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>

            // Find start of dictionary
            var startIndex = content.IndexOf(Constants.DictionaryStart) + 2;
            if (startIndex == -1)
            {
                throw new ParserException(_defaultExceptionMessage);
            }

            // Find end of dictionary
            int countStart = 0;
            int countEnd = 0;
            int i;
            for (i = 0; i < content.Length - 1; i ++)
            {

                // TODO: consider if objects can contain escaped dictionary delimiters which may break this logic, write tests

                var c = content[i..(i + 2)];

                if (c == Constants.DictionaryStart) { countStart++; }
                if (c == Constants.DictionaryEnd) { countEnd++; }

                if (countStart > 0 && countEnd == countStart)
                {
                    break;
                }
            }

            Dictionary<Name, PdfObject> dictionary = new();

            var dictContent = content[startIndex..i].TrimEnd();
            if (string.IsNullOrWhiteSpace(dictContent))
            {
                return new ParseResult<Dictionary>(dictionary, content[(i + 2)..]);
            }

            var items = PdfContentParser.Parse(dictContent).ToArray();

            for (int j = 0; j < items.Length; j += 2)
            {
                dictionary.Add((Name)items[j], items[j + 1]);
            }

            return new ParseResult<Dictionary>(dictionary, content[(i + 2)..]);
        }
    }
}