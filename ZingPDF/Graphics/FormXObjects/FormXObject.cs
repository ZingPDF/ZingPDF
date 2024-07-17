using ZingPDF.ObjectModel.ContentStreamsAndResources;
using ZingPDF.ObjectModel.Filters;

namespace ZingPDF.Graphics.FormXObjects
{
    internal class FormXObject : ContentStream<Type1FormDictionary>
    {
        public FormXObject(IEnumerable<IFilter>? filters) : base(filters)
        {
        }


    }
}
