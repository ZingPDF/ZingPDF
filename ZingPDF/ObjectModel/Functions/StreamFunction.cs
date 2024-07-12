using ZingPDF.ObjectModel.Filters;
using ZingPDF.ObjectModel.Objects.Streams;

namespace ZingPDF.ObjectModel.Functions
{
    internal abstract class StreamFunction<TDictionary> : StreamObject<TDictionary>
        where TDictionary : StreamFunctionDictionary
    {
        protected StreamFunction(IEnumerable<IFilter>? filters) : base(filters)
        {
        }
    }
}
