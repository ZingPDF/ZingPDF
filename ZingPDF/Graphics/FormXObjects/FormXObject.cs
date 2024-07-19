using ZingPDF.ObjectModel.ContentStreamsAndResources;
using ZingPDF.ObjectModel.Filters;

namespace ZingPDF.Graphics.FormXObjects
{
    internal class FormXObject : ContentStream<Type1FormDictionary>
    {
        public FormXObject(
            IEnumerable<IFilter>? filters,
            // TODO
            ) : base(filters)
        {
        }

        protected override Task<Type1FormDictionary> GetSpecialisedDictionaryAsync()
        {
            // TODO

            return Task.FromResult(new Type1FormDictionary());
        }
    }
}
