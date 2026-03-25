using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// Represents a push-button field in an AcroForm.
/// </summary>
/// <remarks>
/// This type is currently useful for discovery and inspection only. Push-button actions are not yet exposed
/// through the high-level API.
/// </remarks>
public class PushButtonFormField : FormField<IPdfObject>
{
    /// <summary>
    /// Initializes a push-button field wrapper.
    /// </summary>
    public PushButtonFormField(
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
