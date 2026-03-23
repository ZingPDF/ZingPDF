using ZingPDF.Extensions;
using ZingPDF.InteractiveFeatures;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Text
{
    public class TextFormField : FormField<PdfString>
    {
        private readonly IParser<ContentStream> _contentStreamParser;

        public TextFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            FieldProperties properties,
            Form parent,
            IPdf pdf,
            IParser<ContentStream> contentStreamParser
            )
            : base(fieldIndirectObject, name, description, properties, parent, pdf)
        {
            _contentStreamParser = contentStreamParser;
        }

        public async Task<string?> GetValueAsync()
        {
            if (_fieldDictionary.V == null)
            {
                return null;
            }

            return (await _fieldDictionary.V.GetAsync() as PdfString)!.Decode();
        }

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

                await new VariableTextAppearanceStreamManager(formDict, _fieldDictionary, _pdf, _contentStreamParser, fontProviders)
                    .WriteTextAsync(pdfValue);
            }

            SetValue(pdfValue);
        }

        // temp methods for testing
        public async Task<ContentStream?> GetAPAsync()
        {
            var test = new VariableTextAppearanceStreamManager(await _parent.GetFormDictionaryAsync(), _fieldDictionary, _pdf, _contentStreamParser, []);

            return await test.GetAPAsync();
        }
        
        public async Task ClearAsync()
        {
            var manager = new VariableTextAppearanceStreamManager(await _parent.GetFormDictionaryAsync(), _fieldDictionary, _pdf, _contentStreamParser, []);

            await manager.WipeFieldAsync();

            _pdf.Objects.Update(_fieldIndirectObject);

            _parent.MarkForUpdate();
        }
    }
}
