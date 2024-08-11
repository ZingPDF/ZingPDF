using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes
{
    public class RadioButtonFormField : FormField<bool>
    {
        public RadioButtonFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            bool value,
            FieldProperties properties,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary
            )
            : base(fieldIndirectObject, name, description, value, properties, parent, indirectObjectDictionary)
        {
        }

        protected internal override ContentStreamObject BuildVisualContent()
        {
            throw new NotImplementedException();
        }
    }
}
