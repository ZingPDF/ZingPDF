using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Filters
{
    internal class JPXDecodeFilter : IFilter
    {
        public Name Name => throw new NotImplementedException();

        public Dictionary? Params => throw new NotImplementedException();

        public MemoryStream Decode(Stream data)
        {
            throw new NotImplementedException();
        }

        public MemoryStream Encode(Stream data)
        {
            throw new NotImplementedException();
        }
    }
}
