using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class IndirectObjectReferenceParser : IPdfObjectParser<IndirectObjectReference>
    {
        public async ITask<IndirectObjectReference> ParseAsync(Stream stream)
        {
            Console.WriteLine($"Parsing IndirectObjectReference from {stream.GetType().Name} at offset: {stream.Position}.");

            var content = await stream.ReadUpToIncludingAsync(Constants.IndirectReference);

            content = content.TrimStart();

            var parts = content.Split(Constants.Whitespace);

            var id = int.Parse(parts[0]);
            var generation = ushort.Parse(parts[1]);

            return new IndirectObjectReference(new(id, generation));
        }
    }
}
