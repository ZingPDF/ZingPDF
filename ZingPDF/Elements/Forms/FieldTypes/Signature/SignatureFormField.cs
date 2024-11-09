using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Signature
{
    public class SignatureFormField : FormField<IPdfObject>
    {
        public SignatureFormField(
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
