using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class IntegerParser : IPdfObjectParser<Integer>
    {
        public Integer Parse(IEnumerable<string> tokens)
            => new(int.Parse(tokens.ElementAt(0)));
    }
}
