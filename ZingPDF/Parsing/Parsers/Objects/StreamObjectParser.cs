using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class StreamObjectParser : IObjectParser<StreamObject<IStreamDictionary>>
    {
        private readonly IStreamDictionary? _dict;
        private readonly IPdfEditor _pdfEditor;

        public StreamObjectParser(IPdfEditor pdfEditor)
        {
            ArgumentNullException.ThrowIfNull(nameof(pdfEditor));

            _pdfEditor = pdfEditor;
        }

        public StreamObjectParser(IPdfEditor pdfEditor, IStreamDictionary dict)
        {
            ArgumentNullException.ThrowIfNull(nameof(pdfEditor));
            ArgumentNullException.ThrowIfNull(nameof(dict));

            _pdfEditor = pdfEditor;
            _dict = dict;
        }

        public async ITask<StreamObject<IStreamDictionary>> ParseAsync(Stream stream)
        {
            var initialStreamPosition = stream.Position;

            //Logger.Log(LogLevel.Trace, $"Parsing StreamObject from {stream.GetType().Name} at offset: {initialStreamPosition}.");

            var dict = _dict ?? (IStreamDictionary)await Parser.For<Dictionary>(_pdfEditor).ParseAsync(stream);

            //var streamDict = dict as IStreamDictionary ?? throw new ParserException("Invalid stream dictionary");

            var streamLength = await dict.Length.GetAsync();

            await stream.AdvanceBeyondNextAsync(Constants.StreamStart);
            stream.AdvancePastWhitepace();

            var streamDataOffset = stream.Position;

            stream.Position += streamLength;

            Logger.Log(LogLevel.Trace, $"Parsed StreamObject. Creating SubStream within {stream.GetType().Name} between: {streamDataOffset} and {streamDataOffset + streamLength}.");

            return new StreamObject<IStreamDictionary>(
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

