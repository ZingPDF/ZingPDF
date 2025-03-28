using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Text
{
    public class TextFormField : FormField<LiteralString>
    {
        private readonly Name _fontResourceName;

        public TextFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            FieldProperties properties,
            Form parent,
            IPdfEditor pdfEditor,
            Name fontResourceName
            )
            : base(fieldIndirectObject, name, description, properties, parent, pdfEditor)
        {
            _fontResourceName = fontResourceName;
        }

        public async Task<string?> GetValueAsync()
        {
            if (_fieldDictionary.V == null)
            {
                return null;
            }

            return await _fieldDictionary.V.GetAsync() as LiteralString;
        }

        public async Task SetValueAsync(string? value)
        {
            var formDict = await _parent.GetFormDictionaryAsync();
            var fontProviders = await _parent.GetFontProvidersAsync();

            if (value == null)
            {
                await ClearAsync();
            }
            else
            {
                await new VariableTextAppearanceStreamManager(formDict, _fieldDictionary, _pdfEditor, fontProviders)
                    .WriteTextAsync(value!);
            }

            SetValue(value);
        }

        // temp methods for testing
        public async Task<ContentStream?> GetAPAsync()
        {
            var test = new VariableTextAppearanceStreamManager(await _parent.GetFormDictionaryAsync(), _fieldDictionary, _pdfEditor, []);

            return await test.GetAPAsync();
        }
        
        public async Task ClearAsync()
        {
            var manager = new VariableTextAppearanceStreamManager(await _parent.GetFormDictionaryAsync(), _fieldDictionary, _pdfEditor, []);

            await manager.WipeFieldAsync();

            _pdfEditor.Update(_fieldIndirectObject);

            _parent.MarkForUpdate();
        }
    }
}
