using MorseCode.ITask;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups;

namespace ZingPdf.Core.Parsing
{
    internal class PdfObjectGroupParser : IPdfObjectParser<PdfObjectGroup>
    {
        public async ITask<PdfObjectGroup> ParseAsync(Stream stream)
        {
            var items = new List<PdfObject>();

            while (stream.Position < stream.Length)
            {
                var type = await TokenTypeIdentifier.TryIdentifyAsync(stream);

                if (type != null)
                {
                    items.Add(await Parser.For(type).ParseAsync(stream));
                }
            }

            return items.ToArray();
        }
    }
}