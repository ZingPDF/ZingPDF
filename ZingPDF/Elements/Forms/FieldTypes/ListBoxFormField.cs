using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes
{
    public class ListBoxFormField : FormField<ArrayObject>
    {
        public ListBoxFormField(
            IndirectObject fieldIndirectObject,
            string name,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary
            )
            : base(fieldIndirectObject, name, parent, indirectObjectDictionary)
        {
        }

        protected override ArrayObject? GetValue()
        {
            throw new NotImplementedException();
        }
    }
}
