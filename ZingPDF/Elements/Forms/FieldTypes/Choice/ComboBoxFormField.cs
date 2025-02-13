using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

internal class ComboBoxFormField : ChoiceFormField
{
    public ComboBoxFormField(
        IndirectObject fieldIndirectObject,
        string name,
        Form parent,
        IPdfEditor pdfEditor
        )
        : base(fieldIndirectObject, name, parent, pdfEditor)
    {
    }

    public void SelectCustomValue(string value)
    {
        SelectOption(value);
    }

    public void DeselectCustomValue(string value)
    {
        DeselectOption(value);
    }
}
