using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Filters;

namespace ZingPDF.Graphics.FormXObjects
{
    /// <summary>
    /// <para>ISO 32000-2:2020 8.10 - Form XObjects</para>
    /// 
    /// <para>
    /// A Form XObject is a reusable content stream. They are used for repeatable graphical elements such as logos.
    /// They are also used as appearance streams to describe the display of annotations. This includes the widget annotations
    /// used to describe the appearance of form fields.
    /// </para>
    /// 
    /// <para>
    /// Form XObjects differ from content streams in that their dictionary contains properties allowing the element to be reused.
    /// A Form XObject will appear as an indirect object for reference from other components.
    /// Regular content streams appear inline.
    /// </para>
    /// </summary>
    internal class FormXObject : ContentStream<Type1FormDictionary>
    {
        private readonly Rectangle _bBox;
        private readonly ResourceDictionary? _resources;

        public FormXObject(
            Rectangle bBox,
            IEnumerable<PdfObject> graphicsObjects,
            ResourceDictionary? resources = null,
            IEnumerable<IFilter>? filters = null
            )
            : base(graphicsObjects, filters)
        {
            _bBox = bBox ?? throw new ArgumentNullException(nameof(bBox));
            _resources = resources;
        }

        protected override Task<Type1FormDictionary> GetSpecialisedDictionaryAsync()
        {
            return Task.FromResult(new Type1FormDictionary(_bBox, _resources));
        }
    }
}
