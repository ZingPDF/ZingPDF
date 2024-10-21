using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Elements.Forms.FieldTypes;

public class Checkbox
{
    private readonly IIndirectObjectDictionary _indirectObjectDictionary;

    internal Checkbox(Name? value, IIndirectObjectDictionary indirectObjectDictionary)
    {
        Value = value;
        
        _indirectObjectDictionary = indirectObjectDictionary ?? throw new ArgumentNullException(nameof(indirectObjectDictionary));
    }

    public bool Checked => Value != Constants.CheckboxStates.NotChecked;

    public Name? Value { get; }

    public void Check()
    {
        _indirectObjectDictionary.EnsureEditable();

        // TODO
    }

    public void Uncheck()
    {
        _indirectObjectDictionary.EnsureEditable();

        // TODO
    }
}