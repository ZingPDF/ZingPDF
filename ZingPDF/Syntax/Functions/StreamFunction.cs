using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.Functions
{
    internal abstract class StreamFunction<TDictionary> : StreamObject<TDictionary>
        where TDictionary : StreamFunctionDictionary
    {
        protected StreamFunction(IEnumerable<IFilter>? filters) : base(filters, false)
        {
        }
    }
}
