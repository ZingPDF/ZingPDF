using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences.CrossReferenceStreams;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.Streams;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class StreamObjectParser : IPdfObjectParser<IStreamObject<IStreamDictionary>>
    {
        public async ITask<IStreamObject<IStreamDictionary>> ParseAsync(Stream stream)
        {
            var dict = await Parser.For<Dictionary>().ParseAsync(stream);

            var streamDict =
                dict as CrossReferenceStreamDictionary as IStreamDictionary
                ?? dict as ObjectStreamDictionary as IStreamDictionary
                ?? StreamDictionary.FromDictionary(dict);

            var streamLength = streamDict.Length!;

            await stream.AdvanceBeyondNextAsync(Constants.StreamStart);
            stream.AdvancePastWhitepace();

            var streamDataOffset = stream.Position;

            stream.Position += streamLength;

            return new SubStreamObject(
                stream,
                streamDataOffset,
                streamDataOffset + streamLength,
                streamDict
                );
        }
    }
}