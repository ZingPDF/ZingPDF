using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Objects.ObjectGroups.CrossReferences.CrossReferenceStreams;
using ZingPDF.Objects.Primitives.Streams;
using ZingPDF.Logging;
using ZingPDF.Objects.Primitives;
using ZingPDF.Parsing;

namespace ZingPDF.Parsing.PrimitiveParsers
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

            Logger.Log(LogLevel.Trace, $"Parsing StreamObject from {stream.GetType().Name} at offset: {initialStreamPosition}.");

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

            Logger.Log(LogLevel.Trace, $"Finished parsing StreamObject from {stream.GetType().Name} at offset: {initialStreamPosition}.");

            return new SubStreamObject(
                stream,
                streamDataOffset,
                streamDataOffset + streamLength,
                streamDict
                );
        }
    }
}