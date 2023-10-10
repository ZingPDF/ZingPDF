using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Parsing
{
    internal static class PdfContentParser
    {
        public static IEnumerable<PdfObject> Parse(string content)
        {
            do
            {
                if (!TokenTypeIdentifier.TryIdentify(content, out var tokenType))
                {
                    throw new ParserException($"Unable to identify token: {content}");
                }

                var result = Parser.For(tokenType).Parse(content);

                yield return result.Obj;

                content = result.RemainingContent;
            }
            while (content.Length > 0); // TODO: exit strategy
        }
    }
}