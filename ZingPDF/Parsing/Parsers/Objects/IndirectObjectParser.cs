using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.Graphics.Images;
using ZingPDF.Logging;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Encryption;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class IndirectObjectParser : IParser<IndirectObject>
    {
        private readonly IParserResolver _parserResolver;
        private readonly IParser<Number> _numberParser;
        private readonly IParser<Keyword> _keywordParser;
        private readonly IParser<Dictionary> _dictionaryParser;
        private readonly IParser<Name> _nameParser;
        private readonly IParser<ArrayObject> _arrayParser;
        private readonly IParser<IndirectObjectReference> _indirectObjectReferenceParser;
        private readonly IParser<PdfString> _pdfStringParser;
        private readonly IParser<BooleanObject> _booleanObjectParser;
        private readonly IParser<Date> _dateParser;
        private readonly ITokenTypeIdentifier _tokenTypeIdentifier;
        private readonly IPdfEncryptionProvider _encryptionProvider;

        public IndirectObjectParser(
            IParserResolver parserResolver,
            IParser<Number> numberParser,
            IParser<Keyword> keywordParser,
            IParser<Dictionary> dictionaryParser,
            IParser<Name> nameParser,
            IParser<ArrayObject> arrayParser,
            IParser<IndirectObjectReference> indirectObjectReferenceParser,
            IParser<PdfString> pdfStringParser,
            IParser<BooleanObject> booleanObjectParser,
            IParser<Date> dateParser,
            ITokenTypeIdentifier tokenTypeIdentifier,
            IPdfEncryptionProvider encryptionProvider)
        {
            _parserResolver = parserResolver;
            _numberParser = numberParser;
            _keywordParser = keywordParser;
            _dictionaryParser = dictionaryParser;
            _nameParser = nameParser;
            _arrayParser = arrayParser;
            _indirectObjectReferenceParser = indirectObjectReferenceParser;
            _pdfStringParser = pdfStringParser;
            _booleanObjectParser = booleanObjectParser;
            _dateParser = dateParser;
            _tokenTypeIdentifier = tokenTypeIdentifier;
            _encryptionProvider = encryptionProvider;
        }

        public async ITask<IndirectObject> ParseAsync(Stream stream, ObjectContext context)
        {
            using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("IndirectObjectParser.ParseAsync");
            stream.AdvancePastWhitepace();

            var initialStreamPosition = stream.Position;

            var id = await _numberParser.ParseAsync(stream, context);
            var genNumber = await _numberParser.ParseAsync(stream, context);

            // obj keyword
            _ = await _keywordParser.ParseAsync(stream, context);

            var parentReference = new IndirectObjectReference(new IndirectObjectId(id, genNumber), ObjectContext.WithOrigin(context.Origin));
            var itemContext = context with { NearestParent = parentReference };
            IPdfObject? item = null;

            while (stream.Position < stream.Length)
            {
                var type = await _tokenTypeIdentifier.TryIdentifyAsync(stream);

                if (type == null)
                {
                    continue;
                }

                item = await ParseSingleItemAsync(type, stream, itemContext);
                break;
            }

            if (item is null)
            {
                throw new ParserException("Unable to parse indirect object value.");
            }

            if (item is IStreamDictionary streamDict)
            {
                var nextType = await _tokenTypeIdentifier.TryIdentifyAsync(stream);
                if (nextType == typeof(StreamObject<>))
                {
                    // It's difficult to reliably identify a stream, which is a dictionary followed by the stream contents.
                    // The token identifier will recognise the stream keyword, at which point we've already parsed the dictionary.
                    item = await ParseTypedStreamObjectAsync(streamDict, stream, itemContext);
                }
            }
            else
            {
                _ = await _keywordParser.ParseAsync(stream, itemContext);
            }

            return new IndirectObject(new IndirectObjectId(id, genNumber), item) { ByteOffset = initialStreamPosition };
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

        private async Task<IPdfObject> ParseSingleItemAsync(Type type, Stream stream, ObjectContext context)
        {
            if (type == typeof(Dictionary))
            {
                return await _dictionaryParser.ParseAsync(stream, context);
            }

            if (type == typeof(Name))
            {
                return await _nameParser.ParseAsync(stream, context);
            }

            if (type == typeof(Number))
            {
                return await _numberParser.ParseAsync(stream, context);
            }

            if (type == typeof(ArrayObject))
            {
                return await _arrayParser.ParseAsync(stream, context);
            }

            if (type == typeof(IndirectObjectReference))
            {
                return await _indirectObjectReferenceParser.ParseAsync(stream, context);
            }

            if (type == typeof(PdfString))
            {
                return await _pdfStringParser.ParseAsync(stream, context);
            }

            if (type == typeof(BooleanObject))
            {
                return await _booleanObjectParser.ParseAsync(stream, context);
            }

            if (type == typeof(Date))
            {
                return await _dateParser.ParseAsync(stream, context);
            }

            if (type == typeof(Keyword))
            {
                return await _keywordParser.ParseAsync(stream, context);
            }

            return await _parserResolver.GetParserFor(type).ParseAsync(stream, context);
        }
    }
}
