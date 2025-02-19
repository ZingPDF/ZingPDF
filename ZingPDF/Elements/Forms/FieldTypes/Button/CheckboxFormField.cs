using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// <para>ISO 32000-2:2020 12.7.5.2.3 - Check boxes</para>
/// </summary>
internal class CheckboxFormField : ButtonOptionsFormField
{
    internal CheckboxFormField(
        IndirectObject fieldIndirectObject,
        string name,
        string? description,
        FieldProperties properties,
        Form parent,
        PdfObjectManager pdfObjectManager,
        IEnumerable<IndirectObject> kids
        )
        : base(fieldIndirectObject, name, description, properties, parent, pdfObjectManager, kids)
    {
    }

    protected override async Task SelectOptionAsync(SelectableOption option)
    {
        // When checked
        // - The checkbox field dictionary value (V) must be updated to the export value of the box
        // - The AS value of the widget annotation of the checked box must also have the same value
        // - The AS values of all other checkboxes in the field must be set to /Off

        // TODO: do we need to check all other checkboxes in the whole document with the same field name?

        SetValue(option.Value);

        foreach (var annot in WidgetAnnotationObjects)
        {
            var widgetDictionary = (WidgetAnnotationDictionary)annot.Object;

            if (option.Value == await GetExportValueAsync(widgetDictionary))
            {
                widgetDictionary.SetAppearanceState(option.Value);
            }
            else
            {
                widgetDictionary.SetAppearanceState(Constants.ButtonStates.Off);
            }

            _pdfObjectManager.Update(annot);
        }
    }

    protected override Task DeselectOptionAsync(SelectableOption option)
    {
        // When unchecked
        // - The checkbox field dictionary value (V) must be updated to /Off
        // - The AS value of this checkbox must be set to /Off

        SetValue(Constants.ButtonStates.Off);

        var widgetAnnotation = (WidgetAnnotationDictionary)option.AssociatedDictionary.Object;

        widgetAnnotation.SetAppearanceState(Constants.ButtonStates.Off);

        _pdfObjectManager.Update(option.AssociatedDictionary);

        return Task.CompletedTask;
    }
}
