using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

internal class ListBoxFormField : ChoiceFormField
{
    public ListBoxFormField(
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
