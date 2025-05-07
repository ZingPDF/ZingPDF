using MorseCode.ITask;
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
    internal class IndirectObjectParser : IObjectParser<IndirectObject>
    {
        private readonly IPdfContext _pdfContext;

        public IndirectObjectParser(IPdfContext pdfContext)
        {
            ArgumentNullException.ThrowIfNull(nameof(pdfContext));

            _pdfContext = pdfContext;
        }

        public async ITask<IndirectObject> ParseAsync(Stream stream, ParseContext context)
        {
            stream.AdvancePastWhitepace();

            //Logger.Log(LogLevel.Trace, $"Parsing IndirectObject from {stream.GetType().Name} at offset: {stream.Position}.");

            var initialStreamPosition = stream.Position;

            var id = await _pdfContext.Parser.Numbers.ParseAsync(stream, context);
            var genNumber = await _pdfContext.Parser.Numbers.ParseAsync(stream, context);

            // obj keyword
            _ = await _pdfContext.Parser.Keywords.ParseAsync(stream, context);

            var items = new List<IPdfObject>();

            do
            {
                var type = await TokenTypeIdentifier.TryIdentifyAsync(stream);

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

                IPdfObject item = await _pdfContext.Parser.For(type).ParseAsync(stream, context);

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

        private async Task<IPdfObject> ParseTypedStreamObjectAsync(IStreamDictionary dict, Stream stream, ParseContext context)
        {
            return dict switch
            {
                CrossReferenceStreamDictionary xrefStreamDict
                    => await new StreamObjectParser<CrossReferenceStreamDictionary>(xrefStreamDict, _pdfContext).ParseAsync(stream, context),

                ObjectStreamDictionary objectStreamDict
                    => await new StreamObjectParser<ObjectStreamDictionary>(objectStreamDict, _pdfContext).ParseAsync(stream, context),

                Type1FormDictionary type1FormDict
                    => await new StreamObjectParser<Type1FormDictionary>(type1FormDict, _pdfContext).ParseAsync(stream, context),

                ImageDictionary imageDict
                    => await new StreamObjectParser<ImageDictionary>(imageDict, _pdfContext).ParseAsync(stream, context),

                StreamDictionary streamDict
                    => await new StreamObjectParser<StreamDictionary>(streamDict, _pdfContext).ParseAsync(stream, context),

                _ => throw new InvalidOperationException($"Unsupported dictionary type: {dict.GetType().Name}"),
            };
        }
    }
}
