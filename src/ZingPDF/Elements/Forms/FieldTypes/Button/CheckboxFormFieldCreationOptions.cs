namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// Configures a newly created checkbox field.
/// </summary>
public sealed class CheckboxFormFieldCreationOptions
{
    /// <summary>
    /// Gets or sets the user-facing field description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the export value written when the checkbox is selected.
    /// </summary>
    public string ExportValue { get; set; } = Constants.ButtonStates.On;

    /// <summary>
    /// Gets or sets whether the checkbox starts in the selected state.
    /// </summary>
    public bool Checked { get; set; }

    internal static CheckboxFormFieldCreationOptions Initialize(Action<CheckboxFormFieldCreationOptions>? configure)
    {
        var options = new CheckboxFormFieldCreationOptions();
        configure?.Invoke(options);

        if (string.IsNullOrWhiteSpace(options.ExportValue))
        {
            options.ExportValue = Constants.ButtonStates.On;
        }

        return options;
    }
}
