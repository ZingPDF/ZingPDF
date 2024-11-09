using ZingPDF.Syntax.Objects;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

public class ChoiceItem
{
    internal ChoiceItem(Name text, Name value)
    {
        ArgumentNullException.ThrowIfNull(text, nameof(text));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        Text = text;
        Value = value;
    }

    public Name Text { get; set; }
    public Name Value { get; set; }
}
