using ZingPDF.Extensions;
using ZingPDF.InteractiveFeatures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Text
{
    public class TextFormField : FormField<LiteralString>
    {
        public TextFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            FieldProperties properties,
            Form parent,
            IPdfContext pdfContext
            )
            : base(fieldIndirectObject, name, description, properties, parent, pdfContext)
        {
        }

        public async Task<string?> GetValueAsync()
        {
            if (_fieldDictionary.V == null)
            {
                return null;
            }

            return (await _fieldDictionary.V.GetAsync() as LiteralString)!.Decode();
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
                await new VariableTextAppearanceStreamManager(formDict, _fieldDictionary, _pdfContext, fontProviders)
                    .WriteTextAsync(value);
            }

            SetValue(value);
        }

        // temp methods for testing
        public async Task<ContentStream?> GetAPAsync()
        {
            var test = new VariableTextAppearanceStreamManager(await _parent.GetFormDictionaryAsync(), _fieldDictionary, _pdfContext, []);

            return await test.GetAPAsync();
        }
        
        public async Task ClearAsync()
        {
            var manager = new VariableTextAppearanceStreamManager(await _parent.GetFormDictionaryAsync(), _fieldDictionary, _pdfContext, []);

            await manager.WipeFieldAsync();

            _pdfContext.Objects.Update(_fieldIndirectObject);

            _parent.MarkForUpdate();
        }
    }
}
