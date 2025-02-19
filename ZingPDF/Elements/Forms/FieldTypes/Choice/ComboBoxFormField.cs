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
        PdfObjectManager pdfObjectManager
        )
        : base(fieldIndirectObject, name, description, properties, parent, pdfObjectManager)
    {
    }

    public Task SelectCustomValueAsync(string value) => SelectOptionAsync(value!);
    public Task DeselectCustomValueAsync(string value) => DeselectOptionAsync(value!);
}
