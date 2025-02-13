using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
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
            Form parent,
            IPdfEditor pdfEditor,
            Name fontResourceName
            )
            : base(fieldIndirectObject, name, parent, pdfEditor)
        {
            _fontResourceName = fontResourceName;
        }

        public string? Value
        {
            get => _fieldDictionary.V as LiteralString;
            set
            {
                AddAppearanceStream(value);
                SetValue(value);
            }
        }

        private void AddAppearanceStream(string? value)
        {
            var fieldDict = (FieldDictionary)_fieldIndirectObject.Object;

            // TODO: do we need to account for fields which already have an appearance stream? or always replace?
            var fieldSizeRect = Rectangle.FromSize(fieldDict.Rect.Width, fieldDict.Rect.Height);

            // TODO: handle combed display

            var visualContent = new TextObject(
                value!,
                fieldSizeRect,
                new Coordinate(2, 5), // TODO: calculate this
                new TextObject.FontOptions(_fontResourceName, 12, RGBColour.Black)
                );

            var apFormXObject = new ContentStreamFactory<Type1FormDictionary>(
                [visualContent],
                new Type1FormDictionary(fieldSizeRect)
                )
                .Create();

            var apIndirectObject = _pdfEditor.Add(apFormXObject);

            fieldDict.SetAppearanceStream(AppearanceDictionary.Create(apIndirectObject.Id.Reference));
        }
    }
}
