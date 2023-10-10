using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class HexadecimalStringParser : IPdfObjectParser<HexadecimalString>
    {
        private readonly string _defaultExceptionMessage = "Invalid hexadecimal string";

        public IParseResult<HexadecimalString> Parse(string content)
        {
            var startIndex = content.IndexOf(Constants.LessThan) + 1;
            if (startIndex == -1)
            {
                throw new ParserException(_defaultExceptionMessage);
            }

            // Find end of string
            int endIndex;
            for (endIndex = startIndex; endIndex < content.Length; endIndex++)
            {
                var c = content[endIndex];

                if (c == Constants.GreaterThan)
                {
                    break;
                }
            }

            return new ParseResult<HexadecimalString>(content[startIndex..endIndex], content[(endIndex + 1)..]);
        }
    }
}
