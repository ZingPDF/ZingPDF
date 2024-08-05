using ZingPDF.Syntax.Objects;

namespace ZingPDF.Syntax.Filters
{
    internal class DCTDecodeFilter : IFilter
    {
        public DCTDecodeFilter(Dictionary? filterParams)
        {
            Params = filterParams;
        }

        public Name Name => Constants.Filters.DCT;

        public Dictionary? Params { get; }

        public byte[] Decode(byte[] data)
        {
            throw new NotImplementedException();
        }

        public byte[] Encode(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
