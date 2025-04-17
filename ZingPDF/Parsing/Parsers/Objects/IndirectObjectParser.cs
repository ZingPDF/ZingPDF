using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.Graphics.Images;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Logging;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Text;
using ZingPDF.Text.SimpleFonts;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class IndirectObjectParser : IObjectParser<IndirectObject>
    {
        private readonly IPdfEditor _pdfEditor;

        public IndirectObjectParser(IPdfEditor pdfEditor)
        {
            ArgumentNullException.ThrowIfNull(nameof(pdfEditor));

            _pdfEditor = pdfEditor;
        }

        public async ITask<IndirectObject> ParseAsync(Stream stream)
        {
            stream.AdvancePastWhitepace();

            //Logger.Log(LogLevel.Trace, $"Parsing IndirectObject from {stream.GetType().Name} at offset: {stream.Position}.");

            var initialStreamPosition = stream.Position;

            var id = await Parser.Numbers.ParseAsync(stream);
            var genNumber = await Parser.Numbers.ParseAsync(stream);

            // obj keyword
            _ = await Parser.Keywords.ParseAsync(stream);

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

                    items.Add(await ParseTypedStreamObjectAsync(streamDict, stream));
                    break;
                }

                IPdfObject item = await Parser.For(type, _pdfEditor).ParseAsync(stream);

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

        private static async Task<IPdfObject> ParseTypedStreamObjectAsync(IStreamDictionary dict, Stream stream)
        {
            return dict switch
            {
                CrossReferenceStreamDictionary xrefStreamDict
                    => await new StreamObjectParser<CrossReferenceStreamDictionary>(xrefStreamDict).ParseAsync(stream),

                ObjectStreamDictionary objectStreamDict
                    => await new StreamObjectParser<ObjectStreamDictionary>(objectStreamDict).ParseAsync(stream),

                Type1FormDictionary type1FormDict
                    => await new StreamObjectParser<Type1FormDictionary>(type1FormDict).ParseAsync(stream),

                ImageDictionary imageDict
                    => await new StreamObjectParser<ImageDictionary>(imageDict).ParseAsync(stream),

                StreamDictionary streamDict
                    => await new StreamObjectParser<StreamDictionary>(streamDict).ParseAsync(stream),

                _ => throw new InvalidOperationException($"Unsupported dictionary type: {dict.GetType().Name}"),
            };
        }
    }
}
