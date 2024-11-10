namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

public class ChoiceItem
{
    private readonly Action<string> _onSelect;
    private readonly Action<string> _onDeselect;

    internal ChoiceItem(string text, string value, bool selected, Action<string> onSelect, Action<string> onDeselect)
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

    public string Text { get; set; }
    public string Value { get; set; }
    public bool Selected { get; }

    public void Select()
    {
        _onSelect(Value);
    }

    public void Deselect()
    {
        _onDeselect(Value);
    }
}
