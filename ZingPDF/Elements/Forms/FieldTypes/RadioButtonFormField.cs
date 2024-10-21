using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes
{
    public class RadioButtonFormField : FormField<BooleanObject>
    {
        public RadioButtonFormField(
            IndirectObject fieldIndirectObject,
            string name,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary
            )
            : base(fieldIndirectObject, name, parent, indirectObjectDictionary)
        {
        }

        protected override BooleanObject? GetValue()
        {
            throw new NotImplementedException();
        }
    }
}
