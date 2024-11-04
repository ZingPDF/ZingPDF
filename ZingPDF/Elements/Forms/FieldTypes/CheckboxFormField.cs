using ZingPDF.Extensions;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes;

/// <summary>
/// ISO 32000-2:2020 12.7.5.2.3 - Check boxes
/// 
/// A CheckboxFormField represents one or more checkboxes.
/// In practice, a single checkbox field with multiple boxes acts as a group of mutually exclusive options, like a radio button group.
/// </summary>
public class CheckboxFormField : FormField<Name>
{
    private readonly IEnumerable<IndirectObject> _kids;

    public CheckboxFormField(
        IndirectObject fieldIndirectObject,
        string name,
        Form parent,
        IIndirectObjectDictionary indirectObjectDictionary,
        IEnumerable<IndirectObject> kids
        )
        : base(fieldIndirectObject, name, parent, indirectObjectDictionary)
    {
        _kids = kids;

        InitCheckboxes();
    }

    public IList<Checkbox> Checkboxes { get; private set; } = [];

    protected override Name? GetValue()
    {
        // TODO: ignoring the Opt array for now, which I think is only used for very rare multi-state checkboxes

        // The V entry contains the 'export' value for the checkbox field.
        // If the box (or all boxes) are unchecked, this will be null or contain /Off.
        // If checked, this will be the unique value for the selected box.
        return _fieldDictionary.V switch
        {
            null => null,
            Name value => value,
            _ => throw new InvalidOperationException(),
        };
    }

    /// <summary>
    /// Sets up a <see cref="Checkbox"/> instance for each child checkbox of this field.
    /// This allows the user to easily check/uncheck each checkbox.
    /// On toggling the checkbox, it will call the supplied `onChange` callback, which we 
    /// configure here to set the value of this instance.
    /// </summary>
    private void InitCheckboxes()
    {
        var fieldValue = GetValue();

        List<Checkbox> checkBoxes = [];

        Checkboxes = CheckboxDictionaries.Select(widgetDict =>
        {
            Name exportValue = GetExportValue(widgetDict);

            var @checked = fieldValue == exportValue;

            return new Checkbox(exportValue, @checked, (val) =>
            {
                _indirectObjectDictionary.EnsureEditable();

                // When checked
                // - The checkbox field dictionary value must be updated to the export value of the box
                // - The AS value of the widget annotation of the checked box must also have the same value
                // - The AS values of all other checkboxes in the field must be set to /Off

                // TODO: figure out how to make each checkbox instance update its checked property to false when another box is checked
                Value = val;

                if (_kids is null)
                {
                    _fieldDictionary.SetAppearanceState(val);
                }
                else
                {
                    foreach (var indirectObject in _kids)
                    {
                        var widgetDict = indirectObject.Get<WidgetAnnotationDictionary>();
                        var exportValue = GetExportValue(widgetDict);

                        widgetDict.SetAppearanceState(Constants.CheckboxStates.NotChecked);

                        if (exportValue == val)
                        {
                            widgetDict.SetAppearanceState(val);
                        }

                        IndirectObjects.Update(indirectObject);
                    }
                }

                // Reset checkbox instances with updated values
                InitCheckboxes();
            });
        }).ToList();
    }

    private static Name GetExportValue(WidgetAnnotationDictionary widgetDict)
    {
        Name value = Constants.CheckboxStates.Checked;

        if (widgetDict.AP is not null)
        {
            if (widgetDict.AP.N is IndirectObject)
            {
                // TODO: handle the case where N is a stream
            }
            else
            {
                value = (widgetDict.AP.N as Dictionary).First().Key;
            }
        }

        return value;
    }

    private IEnumerable<WidgetAnnotationDictionary> CheckboxDictionaries => _kids is null
        ? [_fieldDictionary]
        : _kids.Select(k => k.Get<WidgetAnnotationDictionary>());

    // a) Check the "/Opt" array in the field dictionary.If present, it should contain the export values for the checkbox.The first value is typically the "on" state.
    // b) If "/Opt" is not present, look for the "/AP" (Appearance) dictionary.Under "/N" (Normal), there should be named appearances.One of these (often "/Yes") represents the "on" state.
    // c) If neither of these are present, you can default to "/Yes" as it's a common standard value.
}
