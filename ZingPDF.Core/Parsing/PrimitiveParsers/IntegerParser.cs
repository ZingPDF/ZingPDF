using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class IntegerParser : IPdfObjectParser<Integer>
    {
        public IParseResult<Integer> Parse(string content)
        {
            content = content.TrimStart();

            // Find end of integer
            int endIndex;
            for (endIndex = 0; endIndex < content.Length; endIndex++)
            {
                var c = content[endIndex];

                if (!c.IsInteger())
                {
                    break;
                }
            }

            var remainingContent = content.Length > endIndex ? content[(endIndex + 1)..] : string.Empty;

            return new ParseResult<Integer>(int.Parse(content[..endIndex]), remainingContent);
        }
    }
}
