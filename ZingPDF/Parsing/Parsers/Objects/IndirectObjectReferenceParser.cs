using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class IndirectObjectReferenceParser : IPdfObjectParser<IndirectObjectReference>
    {
        public async ITask<IndirectObjectReference> ParseAsync(Stream stream)
        {
            //Logger.Log(LogLevel.Trace, $"Parsing IndirectObjectReference from {stream.GetType().Name} at offset: {stream.Position}.");

            var content = await stream.ReadUpToIncludingAsync(Constants.IndirectReference);

            content = content.TrimStart();

            var parts = content.Split(Constants.Whitespace);

            var id = int.Parse(parts[0]);
            var generation = ushort.Parse(parts[1]);

            var ior = new IndirectObjectReference(new(id, generation));

            Logger.Log(LogLevel.Trace, $"Parsed IndirectObjectReference: {{{ior}}}. {stream.GetType().Name} now at: {stream.Position}.");

            return ior;
        }
    }
}
