using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// Base class for checkbox and radio-button fields.
/// </summary>
/// <remarks>
/// Call <see cref="GetOptionsAsync"/> to inspect the available choices and then use
/// <see cref="SelectableOption.SelectAsync"/> or <see cref="SelectableOption.DeselectAsync"/> to change selection state.
/// </remarks>
public abstract class ButtonOptionsFormField : FormField<Name>
{
    protected readonly IEnumerable<IndirectObject> _kids;

    // PDF-spec notes kept here for library maintenance:
    // - Button option state is driven by both the field value (/V) and the widget appearance state (/AS).
    // - The "on" export value is typically derived from the appearance dictionary rather than a simple boolean.
    // - Checkbox and radio fields share the same structural model, but differ in how group selection is applied.

    internal ButtonOptionsFormField(
        IndirectObject fieldIndirectObject,
        string name,
        string? description,
        FieldProperties properties,
        Form parent,
        IPdf pdf,
        IEnumerable<IndirectObject> kids
        )
        : base(fieldIndirectObject, name, description, properties, parent, pdf)
    {
        _kids = kids;
    }

    protected abstract Task SelectOptionAsync(SelectableOption option);
    protected abstract Task DeselectOptionAsync(SelectableOption option);

    /// <summary>
    /// Gets the selectable options for the field.
    /// </summary>
    public async Task<IReadOnlyList<SelectableOption>> GetOptionsAsync()
    {
        List<SelectableOption> options = [];

        foreach(var annot in WidgetAnnotationObjects)
        {
            var widgetDict = (WidgetAnnotationDictionary)annot.Object;
            string exportValue = await GetExportValueAsync(widgetDict);

            var @checked = false;

            if (_fieldDictionary.V != null)
            {
                var value = await _fieldDictionary.V.GetAsync();
                @checked = value != null && (Name)value == exportValue;
            }

            options.Add(new SelectableOption(Name, exportValue, @checked, SelectOptionAsync, DeselectOptionAsync, annot));
        }

        return options.AsReadOnly();
    }

    protected async Task<string> GetExportValueAsync(WidgetAnnotationDictionary widgetDict)
    {
        // TODO: consider supporting Opt, which may take precedence for the definition of export values.

        var ap = await widgetDict.AP.GetAsync();
        if (ap == null)
        {
            return Constants.ButtonStates.On;
        }

        // TODO: handle the case where N is a stream

        return ap.Keys.First(k => k != Constants.ButtonStates.Off);
    }

    protected IEnumerable<IndirectObject> WidgetAnnotationObjects
        => !_kids.Any()
            ? [_fieldIndirectObject]
            : _kids;

    protected IEnumerable<WidgetAnnotationDictionary> WidgetAnnotations
        => WidgetAnnotationObjects.Select(k => (WidgetAnnotationDictionary)k.Object);
}
