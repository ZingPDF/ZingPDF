using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Parsing
{
    internal static class PdfContentParser
    {
        public static async IAsyncEnumerable<PdfObject?> ParseAsync(Stream stream)
        {
            while (stream.Position < stream.Length)
            {
                var type = await TokenTypeIdentifier.TryIdentifyAsync(stream);

                yield return type == null
                    ? null
                    : await Parser.For(type).ParseAsync(stream);
            }
        }
    }
}