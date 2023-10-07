using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Parsing.ObjectParsers
{
    internal class IndirectObjectReferenceParser : IPdfObjectParser<IndirectObjectReference>
    {
        public IndirectObjectReference Parse(IEnumerable<string> tokens)
        {
            var parts = tokens.ElementAt(0).Split(Constants.Whitespace);

            var id = int.Parse(parts[0]);
            var generation = int.Parse(parts[1]);

            return new IndirectObjectReference(id, generation);
        }
    }
}
