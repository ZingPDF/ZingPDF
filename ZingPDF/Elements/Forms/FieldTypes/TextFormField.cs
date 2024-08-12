using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Text;

namespace ZingPDF.Elements.Forms.FieldTypes
{
    public class TextFormField : FormField<LiteralString>
    {
        private readonly Name _fontResourceName;

        public TextFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            string? value,
            FieldProperties properties,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary,
            Name fontResourceName
            )
            : base(fieldIndirectObject, name, description, value, properties, parent, indirectObjectDictionary)
        {
            _fontResourceName = fontResourceName;
        }

        protected override void OnChange()
        {
            AddAppearanceStream();
        }

        private void AddAppearanceStream()
        {
            var fieldDict = _fieldIndirectObject.Get<FieldDictionary>();

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
