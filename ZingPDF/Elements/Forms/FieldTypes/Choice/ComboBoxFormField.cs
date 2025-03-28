using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

internal class ComboBoxFormField : ChoiceFormField
{
    public ComboBoxFormField(
        IndirectObject fieldIndirectObject,
        string name,
        string? description,
        FieldProperties properties,
        Form parent,
        IPdfEditor pdfEditor
        )
        : base(fieldIndirectObject, name, description, properties, parent, pdfEditor)
    {
    }

    public Task SelectCustomValueAsync(string value) => SelectOptionAsync(value!);
    public Task DeselectCustomValueAsync(string value) => DeselectOptionAsync(value!);
}
