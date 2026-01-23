using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

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

    public PdfString Text { get; set; }
    public PdfString Value { get; set; }
    public bool Selected { get; }

    public Task SelectAsync() => _onSelect(Value);
    public Task DeselectAsync() => _onDeselect(Value);
}
