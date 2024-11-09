using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice
{
    public class ComboBoxFormField : FormField<ArrayObject>
    {
        public ComboBoxFormField(
            IndirectObject fieldIndirectObject,
            string name,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary
            )
            : base(fieldIndirectObject, name, parent, indirectObjectDictionary)
        {
        }
    }
}
