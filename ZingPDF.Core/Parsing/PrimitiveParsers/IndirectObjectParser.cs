using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class IndirectObjectParser : IPdfObjectParser<IndirectObject>
    {
        public async ITask<IndirectObject> ParseAsync(Stream stream)
        {
            await stream.AdvancePastWhitepaceAsync();

            var integerParser = Parser.For<Integer>();

            var id = await integerParser.ParseAsync(stream);
            var genNumber = await integerParser.ParseAsync(stream);

            // obj keyword
            _ = await Parser.For<Keyword>().ParseAsync(stream);

            var items = new List<PdfObject>();

            do
            {
                var type = await TokenTypeIdentifier.TryIdentifyAsync(stream);

                if (type != null)
                {
                    var item = await Parser.For(type).ParseAsync(stream);

                    if (item is Keyword keyword && keyword == Constants.ObjEnd)
                    {
                        break;
                    }

                    items.Add(item);
                }
            }
            while (stream.Position < stream.Length);

            return new IndirectObject(new IndirectObjectId(id, genNumber), items);
        }
    }
}
