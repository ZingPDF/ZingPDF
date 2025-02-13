using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

internal class ListBoxFormField : ChoiceFormField
{
    public ListBoxFormField(
        IndirectObject fieldIndirectObject,
        string name,
        Form parent,
        IPdfEditor pdfEditor
        )
        : base(fieldIndirectObject, name, parent, pdfEditor)
    {
    }
}
