using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes
{
    public class RadioButtonFormField : FormField<BooleanObject>
    {
        public RadioButtonFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            BooleanObject value,
            FieldProperties properties,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary
            )
            : base(fieldIndirectObject, name, description, value, properties, parent, indirectObjectDictionary)
        {
        }
    }
}
