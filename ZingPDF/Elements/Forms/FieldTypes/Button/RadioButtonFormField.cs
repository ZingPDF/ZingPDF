using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

internal class RadioButtonFormField : ButtonOptionsFormField
{
    public RadioButtonFormField(
        IndirectObject fieldIndirectObject,
        string name,
        Form parent,
        IIndirectObjectDictionary indirectObjectDictionary,
        IEnumerable<IndirectObject> kids
        )
        : base(fieldIndirectObject, name, parent, indirectObjectDictionary, kids)
    {
    }

    protected override void SelectOption(SelectableOption option)
    {
        // When selected
        // - The radio button field dictionary value (V) must be updated to the export value of the button
        // - The AS value of the widget annotation of the selected radio button must also have the same value
        // - If the RadiosInUnison flag is present
        //     - The AS values of all other radio buttons with the same value must be set to the on state
        // - Otherwise
        //     - The AS values of all other radio buttons in the field must be set to /Off

        // TODO: do we need to select all radio buttons in the whole document with the same field name and/or value?

        SetValue(option.Value);

        foreach (var annot in WidgetAnnotationObjects)
        {
            var widgetDictionary = annot.Get<WidgetAnnotationDictionary>();
            var exportValue = GetExportValue(widgetDictionary);

            if (option.Value == exportValue)
            {
                widgetDictionary.SetAppearanceState(option.Value);
            }
            else
            {
                if (Properties.RadiosInUnison && option.Value == exportValue)
                {
                    widgetDictionary.SetAppearanceState(option.Value);
                }
                else
                {
                    widgetDictionary.SetAppearanceState(Constants.ButtonStates.Off);
                }
            }

            IndirectObjects.Update(annot);
        }
    }

    protected override void DeselectOption(SelectableOption option)
    {
        // When deselected
        // - If the NoToggleToOff flag is present
        //     - The radio button value must not change
        //     - The AS values of all other radio buttons must not change
        // - Otherwise
        //     - The radio button field dictionary value (V) must be updated to /Off
        //     - The AS value must be updated to /Off

        if (Properties.NoToggleToOff)
        {
            throw new InvalidOperationException("Attempt to deselect radio button for which NoToggleToOff flag is enabled");
        }

        SetValue(Constants.ButtonStates.Off);

        var widgetAnnotation = option.AssociatedDictionary.Get<WidgetAnnotationDictionary>();

        widgetAnnotation.SetAppearanceState(Constants.ButtonStates.Off);

        IndirectObjects.Update(option.AssociatedDictionary);
    }
}
