using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Signature
{
    /// <summary>
    /// Represents a signature field.
    /// </summary>
    /// <remarks>
    /// This type currently exposes metadata only. Digital signing is not yet implemented through this API.
    /// </remarks>
    public class SignatureFormField : FormField<IPdfObject>
    {
        /// <summary>
        /// Initializes a signature field wrapper.
        /// </summary>
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
