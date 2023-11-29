using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.Streams;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class StreamObjectParser : IPdfObjectParser<StreamObject>
    {
        public async ITask<StreamObject> ParseAsync(Stream stream)
        {
            var streamDict = await Parser.For<Dictionary>().ParseAsync(stream) as StreamDictionary;
            var streamLength = streamDict!.Length;

            await stream.AdvanceBeyondNextAsync(Constants.StreamStart);
            stream.AdvancePastWhitepace();

            stream.Position += streamLength;

            return StreamObject.FromEncodedStream(
                stream,
                stream.Position,
                stream.Position + streamLength,
                (IStreamDictionary)streamDict
                );
        }
    }
}