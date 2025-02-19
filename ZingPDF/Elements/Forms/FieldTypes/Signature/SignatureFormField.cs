using ZingPDF.IncrementalUpdates;
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
            PdfObjectManager pdfObjectManager
            )
            : base(fieldIndirectObject, name, description, properties, parent, pdfObjectManager)
        {
        }
    }
}
