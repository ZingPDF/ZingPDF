namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

/// <summary>
/// Defines a selectable option for a choice field.
/// </summary>
public sealed class ChoiceFieldOption
{
    public ChoiceFieldOption(string text)
        : this(text, text)
    {
    }

    public ChoiceFieldOption(string value, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));

        Value = value;
        Text = text;
    }

    /// <summary>
    /// Gets the stored export value for the option.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the user-visible display text for the option.
    /// </summary>
    public string Text { get; }
}
