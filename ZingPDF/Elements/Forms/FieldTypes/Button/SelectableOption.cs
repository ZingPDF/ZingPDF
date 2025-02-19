using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

internal class SelectableOption
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
        ArgumentNullException.ThrowIfNull(onSelect, nameof(onDeselect));
        ArgumentNullException.ThrowIfNull(associatedDictionary, nameof(associatedDictionary));

        Text = text;
        Value = value;
        Selected = selected;

        _onSelect = onSelect;
        _onDeselect = onDeselect;

        AssociatedDictionary = associatedDictionary;
    }

    public string Text { get; set; }
    public string Value { get; set; }
    public bool Selected { get; }

    internal IndirectObject AssociatedDictionary { get; }

    public Task SelectAsync() => _onSelect(this);
    public Task DeselectAsync() => _onDeselect(this);
}
