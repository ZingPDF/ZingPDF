using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Signature
{
    public class SignatureFormField : FormField<IPdfObject>
    {
        public SignatureFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            FieldProperties properties,
            Form parent,
            IPdf pdf
            )
            : base(fieldIndirectObject, name, description, properties, parent, pdf)
        {
        }
    }
}
