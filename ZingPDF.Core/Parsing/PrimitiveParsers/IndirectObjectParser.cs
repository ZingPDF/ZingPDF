using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;
using ZingPdf.Core.Objects.Primitives.Streams;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class IndirectObjectParser : IPdfObjectParser<IndirectObject>
    {
        public async ITask<IndirectObject> ParseAsync(Stream stream)
        {
            Console.WriteLine($"Parsing IndirectObject from {stream.GetType().Name} at offset: {stream.Position}.");

            stream.AdvancePastWhitepace();

            var integerParser = Parser.For<Integer>();

            var id = await integerParser.ParseAsync(stream);
            var genNumber = await integerParser.ParseAsync(stream);

            // obj keyword
            _ = await Parser.For<Keyword>().ParseAsync(stream);

            var items = new List<IPdfObject>();

            do
            {
                var type = await TokenTypeIdentifier.TryIdentifyAsync(stream);

                if (type == null)
                {
                    continue;
                }

                if (type == typeof(IStreamObject<IStreamDictionary>))
                {
                    // It's difficult to reliably identify a stream, which is a dictionary followed by the stream contents.
                    // The token identifier will recognise the stream keyword, at which point we've already parsed the dictionary.
                    var streamDict = (items.Last() as Dictionary)!;
                    items.RemoveAt(items.Count - 1);

                    items.Add(await new StreamObjectParser(streamDict).ParseAsync(stream));
                    break;
                }

                IPdfObject item = await Parser.For(type).ParseAsync(stream);

                if (item is Keyword keyword && keyword == Constants.ObjEnd)
                {
                    break;
                }

                if (item is IStreamObject<IStreamDictionary>)
                {
                    // TODO: is this reachable?
                    break;
                }

                items.Add(item);
                
            }
            while (stream.Position < stream.Length);

            return new IndirectObject(new IndirectObjectId(id, genNumber), [.. items]);
        }
    }
}
