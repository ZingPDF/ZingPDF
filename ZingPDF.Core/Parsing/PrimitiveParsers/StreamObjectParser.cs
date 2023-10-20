using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class StreamObjectParser : IPdfObjectParser<StreamObject>
    {
        public async ITask<StreamObject> ParseAsync(Stream stream)
        {
            var streamDict = await Parser.For<Dictionary>().ParseAsync(stream);
            var streamLength = streamDict.Get<Integer>("Length")!;

            await stream.AdvanceBeyondNextAsync(Constants.StreamStart);
            await stream.AdvancePastWhitepaceAsync();

            return StreamObject.FromEncodedStream(stream, stream.Position, streamLength, streamDict);
        }
    }
}
