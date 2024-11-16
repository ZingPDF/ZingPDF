using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
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
            IIndirectObjectDictionary indirectObjectDictionary,
            Name fontResourceName
            )
            : base(fieldIndirectObject, name, parent, indirectObjectDictionary)
        {
            _fontResourceName = fontResourceName;
        }

        public string? Value
        {
            get => _fieldDictionary.V as LiteralString;
            set
            {
                SetValue(value);

                AddAppearanceStream();
            }
        }

        private void AddAppearanceStream()
        {
            var fieldDict = (FieldDictionary)_fieldIndirectObject.Object ;

            // TODO: do we need to account for fields which already have an appearance stream? or always replace?
            var fieldSizeRect = Rectangle.FromSize(fieldDict.Rect.Width, fieldDict.Rect.Height);

            // TODO: handle combed display

            var visualContent = new TextObject(
                Value!,
                fieldSizeRect,
                new Coordinate(2, 5), // TODO: calculate this
                new TextObject.FontOptions(_fontResourceName, 12, RGBColour.Black)
                );

            var apFormXObject = new FormXObject(
                fieldSizeRect,
                [visualContent],
                null,
                filters: null,
                sourceDataIsCompressed: false
                );

            var apIndirectObject = IndirectObjects.Add(apFormXObject);

            fieldDict.SetAppearanceStream(AppearanceDictionary.Create(apIndirectObject.Id.Reference));
        }
    }
}
