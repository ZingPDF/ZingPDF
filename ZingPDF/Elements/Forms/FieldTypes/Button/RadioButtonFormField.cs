using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// <para>ISO 32000-2:2020 12.7.5.2.4 - Radio buttons</para>
/// </summary>
internal class RadioButtonFormField : ButtonOptionsFormField
{
    public RadioButtonFormField(
        IndirectObject fieldIndirectObject,
        string name,
        Form parent,
        IPdfEditor pdfEditor,
        IEnumerable<IndirectObject> kids
        )
        : base(fieldIndirectObject, name, parent, pdfEditor, kids)
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
            var widgetDictionary = (WidgetAnnotationDictionary)annot.Object;
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

            _pdfEditor.Update(annot);
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

        var widgetAnnotation = (WidgetAnnotationDictionary)option.AssociatedDictionary.Object;

        widgetAnnotation.SetAppearanceState(Constants.ButtonStates.Off);

        _pdfEditor.Update(option.AssociatedDictionary);
    }
}
