namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

/// <summary>
/// Configures a newly created combo box or list box field.
/// </summary>
public sealed class ChoiceFormFieldCreationOptions
{
    /// <summary>
    /// Gets or sets the user-facing field description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the standard font name used for visible field appearances.
    /// </summary>
    public string FontName { get; set; } = "Helvetica";

    /// <summary>
    /// Gets or sets the font size used for visible field appearances.
    /// </summary>
    public int FontSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the initially selected export value.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets whether a combo box allows user-entered values outside the option list.
    /// </summary>
    public bool AllowCustomValues { get; set; }

    /// <summary>
    /// Gets or sets whether a list box allows multiple selections.
    /// </summary>
    public bool AllowMultipleSelection { get; set; }

    /// <summary>
    /// Gets or sets whether the option list should be flagged as sorted.
    /// </summary>
    public bool SortOptions { get; set; }

    internal static ChoiceFormFieldCreationOptions Initialize(Action<ChoiceFormFieldCreationOptions>? configure)
    {
        var options = new ChoiceFormFieldCreationOptions();
        configure?.Invoke(options);
        return options;
    }
}
