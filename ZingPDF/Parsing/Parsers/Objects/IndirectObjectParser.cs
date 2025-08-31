using Microsoft.Extensions.DependencyInjection;
using MorseCode.ITask;
using System.Net.Http;
using ZingPDF.Extensions;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.Graphics.Images;
using ZingPDF.Logging;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class IndirectObjectParser : IParser<IndirectObject>
    {
        private readonly IParserResolver _parserResolver;
        private readonly ITokenTypeIdentifier _tokenTypeIdentifier;

        public IndirectObjectParser(IParserResolver parserResolver, ITokenTypeIdentifier tokenTypeIdentifier)
        {
            _parserResolver = parserResolver;
            _tokenTypeIdentifier = tokenTypeIdentifier;
        }

        public async ITask<IndirectObject> ParseAsync(Stream stream, ObjectContext context)
        {
            stream.AdvancePastWhitepace();

            var initialStreamPosition = stream.Position;

            var id = await _parserResolver.GetParser<Number>().ParseAsync(stream, context);
            var genNumber = await _parserResolver.GetParser<Number>().ParseAsync(stream, context);

            // obj keyword
            _ = await _parserResolver.GetParser<Keyword>().ParseAsync(stream, context);

            var items = new List<IPdfObject>();

            do
            {
                var type = await _tokenTypeIdentifier.TryIdentifyAsync(stream);

                if (type == null)
                {
                    continue;
                }

                if (type == typeof(StreamObject<>))
                {
                    // It's difficult to reliably identify a stream, which is a dictionary followed by the stream contents.
                    // The token identifier will recognise the stream keyword, at which point we've already parsed the dictionary.
                    var streamDict = (items.Last() as IStreamDictionary)!;
                    items.RemoveAt(items.Count - 1);

                    items.Add(await ParseTypedStreamObjectAsync(streamDict, stream, context));
                    break;
                }

                IPdfObject item = await _parserResolver.GetParserFor(type).ParseAsync(stream, context);

                if (item is Keyword keyword && keyword == Constants.ObjEnd)
                {
                    break;
                }

                items.Add(item);

            }
            while (stream.Position < stream.Length);

            return new IndirectObject(new IndirectObjectId(id, genNumber), items.First()) { ByteOffset = initialStreamPosition };
        }

        private static async Task<IPdfObject> ParseTypedStreamObjectAsync(IStreamDictionary dict, Stream stream, ObjectContext context)
        {
            return dict switch
            {
                CrossReferenceStreamDictionary xrefStreamDict
                    => await new StreamObjectParser<CrossReferenceStreamDictionary>(xrefStreamDict).ParseAsync(stream, context),

                ObjectStreamDictionary objectStreamDict
                    => await new StreamObjectParser<ObjectStreamDictionary>(objectStreamDict).ParseAsync(stream, context),

                Type1FormDictionary type1FormDict
                    => await new StreamObjectParser<Type1FormDictionary>(type1FormDict).ParseAsync(stream, context),

                ImageDictionary imageDict
                    => await new StreamObjectParser<ImageDictionary>(imageDict).ParseAsync(stream, context),

                StreamDictionary streamDict
                    => await new StreamObjectParser<StreamDictionary>(streamDict).ParseAsync(stream, context),

                _ => throw new InvalidOperationException($"Unsupported dictionary type: {dict.GetType().Name}"),
            };
        }
    }
}
