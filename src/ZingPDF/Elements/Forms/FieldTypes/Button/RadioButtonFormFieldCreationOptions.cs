namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// Configures a newly created radio-button field.
/// </summary>
public sealed class RadioButtonFormFieldCreationOptions
{
    /// <summary>
    /// Gets or sets the user-facing field description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the initially selected export value.
    /// </summary>
    public string? SelectedValue { get; set; }

    /// <summary>
    /// Gets or sets whether a selected radio button may be toggled back to Off.
    /// </summary>
    public bool NoToggleToOff { get; set; }

    /// <summary>
    /// Gets or sets whether radio buttons sharing the same export value stay in sync.
    /// </summary>
    public bool RadiosInUnison { get; set; }

    internal static RadioButtonFormFieldCreationOptions Initialize(Action<RadioButtonFormFieldCreationOptions>? configure)
    {
        var options = new RadioButtonFormFieldCreationOptions();
        configure?.Invoke(options);
        return options;
    }
}
