using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

/// <summary>
/// Represents a selectable option in a choice field.
/// </summary>
public class ChoiceItem
{
    private readonly Func<PdfString, Task> _onSelect;
    private readonly Func<PdfString, Task> _onDeselect;

    internal ChoiceItem(PdfString text, PdfString value, bool selected, Func<PdfString, Task> onSelect, Func<PdfString, Task> onDeselect)
    {
        ArgumentNullException.ThrowIfNull(text, nameof(text));
        ArgumentNullException.ThrowIfNull(value, nameof(value));
        ArgumentNullException.ThrowIfNull(onSelect, nameof(onSelect));
        ArgumentNullException.ThrowIfNull(onSelect, nameof(onDeselect));

        Text = text;
        Value = value;
        Selected = selected;

        _onSelect = onSelect;
        _onDeselect = onDeselect;
    }

    /// <summary>
    /// Gets or sets the display text for the option.
    /// </summary>
    public PdfString Text { get; set; }

    /// <summary>
    /// Gets or sets the stored option value written to the field when this item is selected.
    /// </summary>
    public PdfString Value { get; set; }

    /// <summary>
    /// Gets whether the option is currently selected.
    /// </summary>
    public bool Selected { get; }

    /// <summary>
    /// Selects this option.
    /// </summary>
    public Task SelectAsync() => _onSelect(Value);

    /// <summary>
    /// Deselects this option.
    /// </summary>
    public Task DeselectAsync() => _onDeselect(Value);
}
