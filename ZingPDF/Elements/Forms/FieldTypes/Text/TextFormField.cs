using ZingPDF.Elements.Drawing;
using ZingPDF.Extensions;
using ZingPDF.Graphics;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text;

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
            PdfObjectManager pdfObjectManager,
            Name fontResourceName
            )
            : base(fieldIndirectObject, name, description, properties, parent, pdfObjectManager)
        {
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
            await AddAppearanceStreamAsync(value);

            SetValue(value);
        }

        private async Task AddAppearanceStreamAsync(string? value)
        {
            var fieldDict = (FieldDictionary)_fieldIndirectObject.Object;

            // TODO: do we need to account for fields which already have an appearance stream? or always replace?
            var fieldSizeRect = Rectangle.FromSize(
                (await fieldDict.Rect.GetAsync(_pdfObjectManager)).Width,
                (await fieldDict.Rect.GetAsync(_pdfObjectManager)).Height
                );

            // TODO: handle combed display

            var visualContent = new TextObject(
                value!,
                fieldSizeRect,
                new Coordinate(2, 5), // TODO: calculate this
                new TextObject.FontOptions(_fontResourceName, 12, RGBColour.Black)
                );

            // Build content stream object
            var ms = new MemoryStream();

            await visualContent.WriteAsync(ms);
            await ms.WriteWhitespaceAsync();

            var apFormXObject = new StreamObject<Type1FormDictionary>(
                ms, 
                new Type1FormDictionary(
                    bBox: fieldSizeRect,
                    resources: null,
                    length: ms.Length,
                    filter: null,
                    decodeParms: null,
                    f: null,
                    fFilter: null,
                    fDecodeParms: null,
                    dL: ms.Length
                    )
                );

            var apIndirectObject = _pdfObjectManager.Add(apFormXObject);

            fieldDict.SetAppearanceStream(AppearanceDictionary.Create(apIndirectObject.Id.Reference));
        }
    }
}
