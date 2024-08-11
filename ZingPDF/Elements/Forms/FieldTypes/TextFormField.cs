using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Text;

namespace ZingPDF.Elements.Forms.FieldTypes
{
    public class TextFormField : FormField<string>
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

        protected internal override ContentStreamObject BuildVisualContent()
        {
            var fieldDict = _fieldIndirectObject.Get<FieldDictionary>();

            // TODO: do we need to account for fields which already have an appearance stream? or always replace?
            var fieldSizeRect = Rectangle.FromSize(fieldDict.Rect.Width, fieldDict.Rect.Height);

            return new TextObject(
                Value!,
                fieldSizeRect,
                new Coordinate(2, 5), // TODO: calculate this
                new TextObject.FontOptions(_fontResourceName, 12, RGBColour.Black)
                );
        }
    }
}
