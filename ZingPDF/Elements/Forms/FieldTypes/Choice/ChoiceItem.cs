using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

public class ChoiceItem
{
    private readonly Func<LiteralString, Task> _onSelect;
    private readonly Func<LiteralString, Task> _onDeselect;

    internal ChoiceItem(LiteralString text, LiteralString value, bool selected, Func<LiteralString, Task> onSelect, Func<LiteralString, Task> onDeselect)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        ArgumentNullException.ThrowIfNull(onSelect, nameof(onSelect));
        ArgumentNullException.ThrowIfNull(onSelect, nameof(onDeselect));

        Text = text;
        Value = value;
        Selected = selected;

        _onSelect = onSelect;
        _onDeselect = onDeselect;
    }

    public LiteralString Text { get; set; }
    public LiteralString Value { get; set; }
    public bool Selected { get; }

    public Task SelectAsync() => _onSelect(Value);
    public Task DeselectAsync() => _onDeselect(Value);
}
