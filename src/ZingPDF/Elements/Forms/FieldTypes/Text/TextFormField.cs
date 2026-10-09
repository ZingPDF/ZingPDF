using ZingPDF.Extensions;
using ZingPDF.InteractiveFeatures;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Text
{
    /// <summary>
    /// Represents a text form field.
    /// </summary>
    public class TextFormField : FormField<PdfString>
    {
        private readonly IParser<ContentStream> _contentStreamParser;
        private readonly IReadOnlyList<IndirectObject> _widgetObjects;

        /// <summary>
        /// Initializes a text field wrapper.
        /// </summary>
        public TextFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            FieldProperties properties,
            Form parent,
            IPdf pdf,
            IParser<ContentStream> contentStreamParser
            )
            : this(fieldIndirectObject, name, description, properties, parent, pdf, contentStreamParser, null)
        {
        }

        internal TextFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            FieldProperties properties,
            Form parent,
            IPdf pdf,
            IParser<ContentStream> contentStreamParser,
            IEnumerable<IndirectObject>? widgetObjects
            )
            : base(fieldIndirectObject, name, description, properties, parent, pdf)
        {
            _contentStreamParser = contentStreamParser;
            _widgetObjects = widgetObjects?.ToList() ?? [];
        }

        /// <summary>
        /// Gets the current field value as decoded text.
        /// </summary>
        public async Task<string?> GetValueAsync()
        {
            if (_fieldDictionary.V == null)
            {
                return null;
            }

            return (await _fieldDictionary.V.GetAsync() as PdfString)?.Decode();
        }

        /// <summary>
        /// Sets the field value and regenerates the field appearance.
        /// </summary>
        /// <param name="value">The new text value, or <see langword="null"/> to clear the field.</param>
        public async Task SetValueAsync(string? value)
        {
            var formDict = await _parent.GetFormDictionaryAsync();
            var fontProviders = await _parent.GetFontProvidersAsync();
            PdfString? pdfValue = null;

            if (value == null)
            {
                await ClearAsync();
            }
            else
            {
                pdfValue = PdfString.FromTextAuto(value, ObjectContext.FromImplicitOperator);

                foreach (var widget in GetWidgetObjects())
                {
                    await new VariableTextAppearanceStreamManager(formDict, _fieldDictionary, _pdf, _contentStreamParser, fontProviders, widget)
                        .WriteTextAsync(pdfValue);
                    if (!ReferenceEquals(widget, _fieldIndirectObject))
                    {
                        _pdf.Objects.Update(widget);
                    }
                }
            }

            SetValue(pdfValue);
        }

        /// <summary>
        /// Gets the field appearance stream, primarily for diagnostics and testing.
        /// </summary>
        public async Task<ContentStream?> GetAPAsync()
        {
            var widget = GetWidgetObjects().FirstOrDefault();
            var test = new VariableTextAppearanceStreamManager(await _parent.GetFormDictionaryAsync(), _fieldDictionary, _pdf, _contentStreamParser, [], widget);

            return await test.GetAPAsync();
        }
        
        /// <summary>
        /// Clears the field value and wipes its appearance stream.
        /// </summary>
        public async Task ClearAsync()
        {
            foreach (var widget in GetWidgetObjects())
            {
                var manager = new VariableTextAppearanceStreamManager(await _parent.GetFormDictionaryAsync(), _fieldDictionary, _pdf, _contentStreamParser, [], widget);
                await manager.WipeFieldAsync();
                if (!ReferenceEquals(widget, _fieldIndirectObject))
                {
                    _pdf.Objects.Update(widget);
                }
            }

            _pdf.Objects.Update(_fieldIndirectObject);

            _parent.MarkForUpdate();
        }

        private IEnumerable<IndirectObject> GetWidgetObjects()
            => _widgetObjects.Count == 0 ? [_fieldIndirectObject] : _widgetObjects;
    }
}
