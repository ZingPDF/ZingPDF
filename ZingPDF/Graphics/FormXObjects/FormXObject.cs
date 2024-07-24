using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Filters;

namespace ZingPDF.Graphics.FormXObjects
{
    internal class FormXObject : ContentStream<Type1FormDictionary>
    {
        private readonly Rectangle _bBox;

        public FormXObject(
            Rectangle bBox,
            IEnumerable<PdfObject> graphicsObjects,
            IEnumerable<IFilter>? filters = null
            )
            : base(graphicsObjects, filters)
        {
            _bBox = bBox ?? throw new ArgumentNullException(nameof(bBox));
        }

        protected override Task<Type1FormDictionary> GetSpecialisedDictionaryAsync()
        {
            return Task.FromResult(new Type1FormDictionary(_bBox));
        }
    }
}
