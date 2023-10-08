using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Parsing.ObjectParsers
{
    internal class IndirectObjectReferenceParser : IPdfObjectParser<IndirectObjectReference>
    {
        public IndirectObjectReference Parse(string content)
        {
            var parts = content.Split(Constants.Whitespace);

            var id = int.Parse(parts[0]);
            var generation = int.Parse(parts[1]);

            return new IndirectObjectReference(id, generation);
        }
    }
}
