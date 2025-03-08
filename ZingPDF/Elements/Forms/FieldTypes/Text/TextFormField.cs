using ZingPDF.Graphics;
using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Text
{
    public class TextFormField : FormField<LiteralString>
    {
        private readonly Dictionary _defaultResources;
        private readonly Name _fontResourceName;

        public TextFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            FieldProperties properties,
            Form parent,
            PdfObjectManager pdfObjectManager,
            Dictionary defaultResources,
            Name fontResourceName
            )
            : base(fieldIndirectObject, name, description, properties, parent, pdfObjectManager)
        {
            _defaultResources = defaultResources;
            _fontResourceName = fontResourceName;
        }

        public async Task<string?> GetValueAsync()
        {
            if (_fieldDictionary.V == null)
            {
                return null;
            }

            return await _fieldDictionary.V.GetAsync(_pdfObjectManager) as LiteralString;
        }

        public async Task SetValueAsync(string? value)
        {
            await new AppearanceGeneration(_pdfObjectManager)
                .SetAppearanceStreamForTextAsync(
                    _fieldDictionary,
                    value ?? string.Empty,
                    defaultResources: _defaultResources,
                    defaultFontResource: _fontResourceName,
                    defaultFontSize: 12,
                    defaultFontColour: RGBColour.Black
                    );

            SetValue(value);
        }
    }
}
