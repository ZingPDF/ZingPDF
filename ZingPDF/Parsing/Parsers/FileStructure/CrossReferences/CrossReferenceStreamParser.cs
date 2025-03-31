using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
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
    /// The usual stream object parser (<see cref="Objects.StreamObjectParser"/>) uses the <see cref="IPdfEditor"/> to find any indirect objects if required.
    /// Some xref streams use an indirect object for the Length property, which we need to create a SubStream.
    /// </remarks>
    internal class CrossReferenceStreamParser : IObjectParser<StreamObject<CrossReferenceStreamDictionary>>
    {
        public async ITask<StreamObject<CrossReferenceStreamDictionary>> ParseAsync(Stream stream)
        {
            var initialStreamPosition = stream.Position;

            var dict = await new CrossReferenceStreamDictionaryParser().ParseAsync(stream) as CrossReferenceStreamDictionary
                ?? throw new ParserException("Failed to parse xref stream");

            Number streamLength = 0;

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

                streamLength = await Parser.Numbers.ParseAsync(stream);

                stream.Position = position;
            }

            await stream.AdvanceBeyondNextAsync(Constants.StreamStart);
            stream.AdvancePastWhitepace();

            var streamDataOffset = stream.Position;

            stream.Position += streamLength;

            Logger.Log(LogLevel.Trace, $"Parsed StreamObject. Creating SubStream within {stream.GetType().Name} between: {streamDataOffset} and {streamDataOffset + streamLength}.");

            return new StreamObject<CrossReferenceStreamDictionary>(
                new SubStream(
                    stream,
                    streamDataOffset,
                    streamDataOffset + streamLength,
                    setToStart: false
                    ),
                dict
                )
            {
                ByteOffset = initialStreamPosition
            };
        }
    }
}
