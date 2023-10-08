using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    internal class IntegerParser : IPdfObjectParser<Integer>
    {
        public Integer Parse(string content)
            => new(int.Parse(content));
    }
}
