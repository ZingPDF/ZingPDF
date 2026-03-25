using Microsoft.Extensions.DependencyInjection;
using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Encryption;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    /// <summary>
    /// Special parser for xref streams.
    /// </summary>
    /// <remarks>
    /// The usual stream object parser (`StreamObjectParser&lt;TDictionary&gt;`) uses the <see cref="IPdfObjectCollection"/> to find any indirect objects if required.
    /// Some xref streams use an indirect object for the Length property, which we need to create a SubStream.
    /// </remarks>
    internal class CrossReferenceStreamParser : IParser<StreamObject<CrossReferenceStreamDictionary>>
    {
        private readonly IPdf _pdf;
        private readonly IParserResolver _parserRegistry;
        private readonly IParser<Number> _numberParser;
        private readonly IPdfEncryptionProvider _encryptionProvider;

        public CrossReferenceStreamParser(
            IPdf pdf,
            IParserResolver parserRegistry,
            IParser<Number> numberParser,
            IPdfEncryptionProvider encryptionProvider
            )
        {
            _pdf = pdf;
            _parserRegistry = parserRegistry;
            _numberParser = numberParser;
            _encryptionProvider = encryptionProvider;
        }

        public async ITask<StreamObject<CrossReferenceStreamDictionary>> ParseAsync(Stream stream, ObjectContext context)
        {
            var initialStreamPosition = stream.Position;

            var dict = await new CrossReferenceStreamDictionaryParser(_pdf, _parserRegistry).ParseAsync(stream, context)
                ?? throw new ParserException("Failed to parse xref stream");

            long streamLength = 0;

            var lengthValue = await dict.Length.GetRawValueAsync();

            if (lengthValue is Number sl)
            {
                streamLength = sl;
            }
            else if (lengthValue is IndirectObjectReference ior)
            {
                // Search forward for the length object, parse.
                var position = stream.Position;

                var indirectObjectMarker = $"{ior.Id.Index} {ior.Id.GenerationNumber} obj";

                var finder = new ObjectFinder();
                var location = await finder.FindAsync(stream, indirectObjectMarker)
                    ?? throw new ParserException($"Unable to locate xref stream length object: {indirectObjectMarker}");

                stream.Position = location;

                streamLength = await _numberParser.ParseAsync(stream, context);

                stream.Position = position;
            }

            await stream.AdvanceBeyondNextAsync(Constants.StreamStart);
            stream.AdvancePastWhitepace();

            var streamDataOffset = stream.Position;

            stream.Position += streamLength;

            return new StreamObject<CrossReferenceStreamDictionary>(
                new SubStream(
                    stream,
                    streamDataOffset,
                    streamDataOffset + streamLength,
                    setToStart: false
                    ),
                dict,
                context,
                _encryptionProvider
                )
            {
                ByteOffset = initialStreamPosition
            };
        }
    }
}
