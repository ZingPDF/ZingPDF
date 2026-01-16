using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.Graphics.Images;
using ZingPDF.Logging;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Encryption;
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
        private readonly IPdfEncryptionProvider _encryptionProvider;

        public IndirectObjectParser(
            IParserResolver parserResolver,
            ITokenTypeIdentifier tokenTypeIdentifier,
            IPdfEncryptionProvider encryptionProvider)
        {
            _parserResolver = parserResolver;
            _tokenTypeIdentifier = tokenTypeIdentifier;
            _encryptionProvider = encryptionProvider;
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
            var parentReference = new IndirectObjectReference(new IndirectObjectId(id, genNumber), ObjectContext.WithOrigin(context.Origin));
            var itemContext = context with { NearestParent = parentReference };

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

                    items.Add(await ParseTypedStreamObjectAsync(streamDict, stream, itemContext));
                    break;
                }

                IPdfObject item = await _parserResolver.GetParserFor(type).ParseAsync(stream, itemContext);

                if (item is Keyword keyword && keyword == Constants.ObjEnd)
                {
                    break;
                }

                items.Add(item);

            }
            while (stream.Position < stream.Length);

            return new IndirectObject(new IndirectObjectId(id, genNumber), items.First()) { ByteOffset = initialStreamPosition };
        }

        private async Task<IPdfObject> ParseTypedStreamObjectAsync(IStreamDictionary dict, Stream stream, ObjectContext context)
        {
            return dict switch
            {
                CrossReferenceStreamDictionary xrefStreamDict
                    => await new StreamObjectParser<CrossReferenceStreamDictionary>(xrefStreamDict, _encryptionProvider).ParseAsync(stream, context),

                ObjectStreamDictionary objectStreamDict
                    => await new StreamObjectParser<ObjectStreamDictionary>(objectStreamDict, _encryptionProvider).ParseAsync(stream, context),

                Type1FormDictionary type1FormDict
                    => await new StreamObjectParser<Type1FormDictionary>(type1FormDict, _encryptionProvider).ParseAsync(stream, context),

                ImageDictionary imageDict
                    => await new StreamObjectParser<ImageDictionary>(imageDict, _encryptionProvider).ParseAsync(stream, context),

                StreamDictionary streamDict
                    => await new StreamObjectParser<StreamDictionary>(streamDict, _encryptionProvider).ParseAsync(stream, context),

                _ => throw new InvalidOperationException($"Unsupported dictionary type: {dict.GetType().Name}"),
            };
        }
    }
}
