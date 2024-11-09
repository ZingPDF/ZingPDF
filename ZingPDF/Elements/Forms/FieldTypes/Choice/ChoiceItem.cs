namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

public class ChoiceItem
{
    private readonly Action<string> _onSelect;

    internal ChoiceItem(string text, string value, Action<string> onSelect)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        ArgumentNullException.ThrowIfNull(onSelect, nameof(onSelect));

        Text = text;
        Value = value;
        _onSelect = onSelect;
    }

    public string Text { get; set; }
    public string Value { get; set; }

    public void Select()
    {
        _onSelect(Value);
    }
}
