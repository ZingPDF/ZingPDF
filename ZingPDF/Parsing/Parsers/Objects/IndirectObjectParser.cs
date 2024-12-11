using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class IndirectObjectParser : IPdfObjectParser<IndirectObject>
    {
        public async ITask<IndirectObject> ParseAsync(Stream stream, IIndirectObjectDictionary indirectObjectDictionary)
        {
            stream.AdvancePastWhitepace();

            //Logger.Log(LogLevel.Trace, $"Parsing IndirectObject from {stream.GetType().Name} at offset: {stream.Position}.");

            var initialStreamPosition = stream.Position;

            var id = await Parser.Integers.ParseAsync(stream, indirectObjectDictionary);
            var genNumber = await Parser.Integers.ParseAsync(stream, indirectObjectDictionary);

            // obj keyword
            _ = await Parser.Keywords.ParseAsync(stream, indirectObjectDictionary);

            var items = new List<IPdfObject>();

            do
            {
                var type = await TokenTypeIdentifier.TryIdentifyAsync(stream);

                if (type == null)
                {
                    continue;
                }

                if (type == typeof(StreamObject<IStreamDictionary>))
                {
                    // It's difficult to reliably identify a stream, which is a dictionary followed by the stream contents.
                    // The token identifier will recognise the stream keyword, at which point we've already parsed the dictionary.
                    var streamDict = (items.Last() as Dictionary)!;
                    items.RemoveAt(items.Count - 1);

                    items.Add(await new StreamObjectParser(streamDict).ParseAsync(stream, indirectObjectDictionary));
                    break;
                }

                IPdfObject item = await Parser.For(type).ParseAsync(stream, indirectObjectDictionary);

                if (item is Keyword keyword && keyword == Constants.ObjEnd)
                {
                    break;
                }

                items.Add(item);

            }
            while (stream.Position < stream.Length);

            Logger.Log(LogLevel.Trace, $"Parsed IndirectObject {{{id.Value} {genNumber.Value} obj}} between offsets: {initialStreamPosition} - {stream.Position}.");

            return new IndirectObject(new IndirectObjectId(id, genNumber), items.First()) { ByteOffset = initialStreamPosition };
        }
    }
}
