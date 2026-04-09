namespace ZingPDF.Elements.Forms.FieldTypes.Signature;

/// <summary>
/// Configures a newly created signature field.
/// </summary>
public sealed class SignatureFormFieldCreationOptions
{
    /// <summary>
    /// Gets or sets the user-facing field description.
    /// </summary>
    public string? Description { get; set; }

    internal static SignatureFormFieldCreationOptions Initialize(Action<SignatureFormFieldCreationOptions>? configure)
    {
        var options = new SignatureFormFieldCreationOptions();
        configure?.Invoke(options);
        return options;
    }
}
