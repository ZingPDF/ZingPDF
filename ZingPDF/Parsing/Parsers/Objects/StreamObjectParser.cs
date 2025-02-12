using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class StreamObjectParser : IObjectParser<StreamObject<IStreamDictionary>>
    {
        private readonly Dictionary? _dict;
        private readonly IIndirectObjectDictionary _indirectObjectDictionary;

        public StreamObjectParser(IIndirectObjectDictionary indirectObjectDictionary)
        {
            ArgumentNullException.ThrowIfNull(nameof(indirectObjectDictionary));

            _indirectObjectDictionary = indirectObjectDictionary;
        }

        public StreamObjectParser(IIndirectObjectDictionary indirectObjectDictionary, Dictionary dict)
        {
            ArgumentNullException.ThrowIfNull(nameof(indirectObjectDictionary));
            ArgumentNullException.ThrowIfNull(nameof(dict));

            _indirectObjectDictionary = indirectObjectDictionary;
            _dict = dict;
        }

        public async ITask<StreamObject<IStreamDictionary>> ParseAsync(Stream stream)
        {
            var initialStreamPosition = stream.Position;

            //Logger.Log(LogLevel.Trace, $"Parsing StreamObject from {stream.GetType().Name} at offset: {initialStreamPosition}.");

            var dict = _dict ?? await Parser.Dictionaries.ParseAsync(stream);

            var streamDict =
                dict as CrossReferenceStreamDictionary as IStreamDictionary
                ?? dict as ObjectStreamDictionary as IStreamDictionary
                ?? StreamDictionary.FromDictionary(dict);

            var streamLength = await streamDict.Length.ResolveAsync<Integer>(_indirectObjectDictionary!);

            await stream.AdvanceBeyondNextAsync(Constants.StreamStart);
            stream.AdvancePastWhitepace();

            var streamDataOffset = stream.Position;

            stream.Position += streamLength;

            Logger.Log(LogLevel.Trace, $"Parsed StreamObject. Creating SubStream within {stream.GetType().Name} between: {streamDataOffset} and {streamDataOffset + streamLength}.");

            var filters = FilterFactory.CreateFilterInstances(streamDict);

            return new StreamObject<IStreamDictionary>(
                new StreamData(
                    new SubStream(
                        stream,
                        streamDataOffset,
                        streamDataOffset + streamLength,
                        setToStart: false
                        ),
                        dataIsCompressed: filters?.Any() ?? false,
                        filters
                    ), streamDict
                )
            {
                ByteOffset = initialStreamPosition
            };
        }
    }
}

