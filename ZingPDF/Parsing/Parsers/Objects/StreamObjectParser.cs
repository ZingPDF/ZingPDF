using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class StreamObjectParser<TDictionary>
        where TDictionary : class, IStreamDictionary
    {
        private readonly TDictionary _dict;

        public StreamObjectParser(TDictionary dict)
        {
            ArgumentNullException.ThrowIfNull(nameof(dict));

            _dict = dict;
        }

        public async ITask<StreamObject<TDictionary>> ParseAsync(Stream stream, ObjectContext context)
        {
            var initialStreamPosition = stream.Position;

            //Logger.Log(LogLevel.Trace, $"Parsing StreamObject from {stream.GetType().Name} at offset: {initialStreamPosition}.");

            var streamLength = await _dict.Length.GetAsync();

            await stream.AdvanceBeyondNextAsync(Constants.StreamStart);
            stream.AdvancePastWhitepace();

            var streamDataOffset = stream.Position;

            stream.Position += streamLength;

            Logger.Log(LogLevel.Trace, $"Parsed StreamObject. Creating SubStream within {stream.GetType().Name} between: {streamDataOffset} and {streamDataOffset + streamLength}.");

            return new StreamObject<TDictionary>(
                new SubStream(
                    stream,
                    streamDataOffset,
                    streamDataOffset + streamLength,
                    setToStart: false
                    ),
                _dict,
                context
                )
            {
                ByteOffset = initialStreamPosition
            };
        }
    }
}

