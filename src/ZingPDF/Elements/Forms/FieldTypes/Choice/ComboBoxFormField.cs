using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

internal class ComboBoxFormField : ChoiceFormField
{
    public ComboBoxFormField(
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

    public Task SelectCustomValueAsync(string value)
        => SelectOptionAsync(PdfString.FromTextAuto(value, ObjectContext.FromImplicitOperator));

    public Task DeselectCustomValueAsync(string value)
        => DeselectOptionAsync(PdfString.FromTextAuto(value, ObjectContext.FromImplicitOperator));
}
