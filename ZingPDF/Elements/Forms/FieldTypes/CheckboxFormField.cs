using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes;

/// <summary>
/// ISO 32000-2:2020 12.7.5.2.3 - Check boxes
/// 
/// A CheckboxFormField represents one or more checkboxes.
/// </summary>
public class CheckboxFormField : FormField<ArrayObject>
{
    public CheckboxFormField(
        IndirectObject fieldIndirectObject,
        string name,
        Form parent,
        IIndirectObjectDictionary indirectObjectDictionary
        )
        : base(fieldIndirectObject, name, parent, indirectObjectDictionary)
    {
        InitCheckboxes();
    }

    public IEnumerable<Checkbox> Checkboxes { get; private set; } = [];

    protected override ArrayObject? GetValue()
    {
        // TODO: ignoring the Opt array for now, which I think is only used for very rare multi-state checkboxes

        switch (_fieldDictionary.V)
        {
            case null:
                return null;
            case Name value:
                return [value];
            case ArrayObject values:
                return values;
            default:
                throw new InvalidOperationException();
        }
    }

    private void InitCheckboxes()
    {
        var values = GetValue() ?? ArrayObject.Empty;

        List<Checkbox> checkBoxes = [];

        if (_fieldDictionary.Kids is null)
        {
            var value = values.Get<Name>(0);

            checkBoxes.Add(new Checkbox(value, _indirectObjectDictionary));
        }
        else
        {
            checkBoxes.AddRange(_fieldDictionary.Kids.Select((k, i) =>
            {
                var value = values.ElementAtOrDefault(i) as Name;

                return new Checkbox(value, _indirectObjectDictionary);
            }));
        }

        Checkboxes = checkBoxes;
    }

    // a) Check the "/Opt" array in the field dictionary.If present, it should contain the export values for the checkbox.The first value is typically the "on" state.
    // b) If "/Opt" is not present, look for the "/AP" (Appearance) dictionary.Under "/N" (Normal), there should be named appearances.One of these (often "/Yes") represents the "on" state.
    // c) If neither of these are present, you can default to "/Yes" as it's a common standard value.
}
