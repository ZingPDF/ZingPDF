using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

internal class ListBoxFormField : ChoiceFormField
{
    public ListBoxFormField(
        IndirectObject fieldIndirectObject,
        string name,
        Form parent,
        IIndirectObjectDictionary indirectObjectDictionary
        )
        : base(fieldIndirectObject, name, parent, indirectObjectDictionary)
    {
    }
}
