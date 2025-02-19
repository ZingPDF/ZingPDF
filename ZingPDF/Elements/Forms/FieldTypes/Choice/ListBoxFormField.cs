using ZingPDF.IncrementalUpdates;
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
        PdfObjectManager pdfObjectManager
        )
        : base(fieldIndirectObject, name, description, properties, parent, pdfObjectManager)
    {
    }
}
