using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// <para>ISO 32000-2:2020 12.7.5.2.2 - Push-buttons</para>
/// </summary>
/// <summary>
/// Represents a push-button field.
/// </summary>
/// <remarks>
/// Push-button actions are not currently exposed through the high-level API.
/// </remarks>
public class PushButtonFormField : FormField<IPdfObject>
{
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
