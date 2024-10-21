using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes
{
    public class PushButtonFormField : FormField<IPdfObject> // TODO
    {
        public PushButtonFormField(
            IndirectObject fieldIndirectObject,
            string name,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary
            )
            : base(fieldIndirectObject, name, parent, indirectObjectDictionary)
        {
        }

        protected override IPdfObject? GetValue()
        {
            throw new NotImplementedException();
        }
    }
}
