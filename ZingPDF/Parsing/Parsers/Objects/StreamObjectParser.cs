using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class StreamObjectParser : IPdfObjectParser<IStreamObject<IStreamDictionary>>
    {
        private readonly Dictionary? _dict;

        public StreamObjectParser()
        {
        }

        public StreamObjectParser(Dictionary dict)
        {
            _dict = dict ?? throw new ArgumentNullException(nameof(dict));
        }

        public async ITask<IStreamObject<IStreamDictionary>> ParseAsync(Stream stream)
        {
            var initialStreamPosition = stream.Position;

            //Logger.Log(LogLevel.Trace, $"Parsing StreamObject from {stream.GetType().Name} at offset: {initialStreamPosition}.");

            var dict = _dict ?? await Parser.For<Dictionary>().ParseAsync(stream);

            var streamDict =
                dict as CrossReferenceStreamDictionary as IStreamDictionary
                ?? dict as ObjectStreamDictionary as IStreamDictionary
                ?? StreamDictionary.FromDictionary(dict);

            var streamLength = streamDict.Length!;

            await stream.AdvanceBeyondNextAsync(Constants.StreamStart);
            stream.AdvancePastWhitepace();

            var streamDataOffset = stream.Position;

            stream.Position += streamLength;

            Logger.Log(LogLevel.Trace, $"Parsed StreamObject. Creating SubStream within {stream.GetType().Name} between: {streamDataOffset} and {streamDataOffset + streamLength}.");

            return new SubStreamObject(
                stream,
                streamDataOffset,
                streamDataOffset + streamLength,
                streamDict
                )
            { ByteOffset = initialStreamPosition };
        }
    }
}