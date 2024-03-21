using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Objects.Primitives.IndirectObjects;
using ZingPDF.Parsing;

namespace ZingPDF.Parsing.PrimitiveParsers
{
    internal class IndirectObjectReferenceParser : IPdfObjectParser<IndirectObjectReference>
    {
        public async ITask<IndirectObjectReference> ParseAsync(Stream stream)
        {
            var content = await stream.ReadUpToIncludingAsync(Constants.IndirectReference);

            content = content.TrimStart();

            var parts = content.Split(Constants.Whitespace);

            var id = int.Parse(parts[0]);
            var generation = ushort.Parse(parts[1]);

            var ior = new IndirectObjectReference(new(id, generation));

            Logger.Log(Logging.LogLevel.Trace, $"Parsed {ior} from {stream.GetType().Name} at offset: {stream.Position}.");

            return ior;
        }
    }
}
