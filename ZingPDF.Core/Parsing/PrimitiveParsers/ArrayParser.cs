using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class ArrayParser : IPdfObjectParser<Objects.Primitives.Array>
    {
        private static string _defaultExceptionMessage = "Invalid array";

        public IParseResult<Objects.Primitives.Array> Parse(string content)
        {
            // An array is a collection of any type of PDF object

            // Find start of array
            var startIndex = content.IndexOf(Constants.ArrayStart) + 1;
            if (startIndex == -1)
            {
                throw new ParserException(_defaultExceptionMessage);
            }

            // Find end of array
            int countStart = 0;
            int countEnd = 0;
            int i;
            for (i = 0; i < content.Length; i++)
            {
                // TODO: consider if objects can contain escaped array delimiters which may break this logic, write tests

                char c = content[i];

                if (c == Constants.ArrayStart) { countStart++; }
                if (c == Constants.ArrayEnd) {  countEnd++; }

                if (countStart > 0 && countEnd == countStart)
                {
                    break;
                }
            }

            var arrayContent = content[startIndex..i].TrimEnd();

            if (string.IsNullOrWhiteSpace(arrayContent))
            {
                return new ParseResult<Objects.Primitives.Array>(Array.Empty<PdfObject>(), content[(i + 1)..]);
            }

            var items = PdfContentParser.Parse(content[startIndex..i].TrimEnd()).ToArray();

            return new ParseResult<Objects.Primitives.Array>(items, content[(i + 1)..]);
        }
    }
}
