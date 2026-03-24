using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// Represents a selectable checkbox or radio-button option.
/// </summary>
public class SelectableOption
{
    private readonly Func<SelectableOption, Task> _onSelect;
    private readonly Func<SelectableOption, Task> _onDeselect;

    internal SelectableOption(
        string text,
        string value,
        bool selected,
        Func<SelectableOption, Task> onSelect,
        Func<SelectableOption, Task> onDeselect,
        IndirectObject associatedDictionary
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        ArgumentNullException.ThrowIfNull(onSelect, nameof(onSelect));
        ArgumentNullException.ThrowIfNull(onDeselect, nameof(onDeselect));
        ArgumentNullException.ThrowIfNull(associatedDictionary, nameof(associatedDictionary));

        Text = text;
        Value = value;
        Selected = selected;

        _onSelect = onSelect;
        _onDeselect = onDeselect;

        AssociatedDictionary = associatedDictionary;
    }

    /// <summary>
    /// Gets or sets the display text for the option.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the export value for the option.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Gets whether the option is currently selected.
    /// </summary>
    public bool Selected { get; }

    internal IndirectObject AssociatedDictionary { get; }

    /// <summary>
    /// Selects this option.
    /// </summary>
    public Task SelectAsync() => _onSelect(this);

    /// <summary>
    /// Deselects this option.
    /// </summary>
    public Task DeselectAsync() => _onDeselect(this);
}
