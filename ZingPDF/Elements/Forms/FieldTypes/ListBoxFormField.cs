using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes
{
    public class ListBoxFormField : FormField<ArrayObject>
    {
        public ListBoxFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            ArrayObject? value,
            FieldProperties properties,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary
            )
            : base(fieldIndirectObject, name, description, value, properties, parent, indirectObjectDictionary)
        {
        }
    }
}
