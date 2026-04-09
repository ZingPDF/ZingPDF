using ZingPDF.Fonts;

namespace ZingPDF.Elements.Forms.FieldTypes.Text;

/// <summary>
/// Options for creating a new AcroForm text field.
/// </summary>
public sealed class TextFormFieldCreationOptions
{
    /// <summary>
    /// Optional user-facing description or tooltip.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// One of the standard PDF fonts used for the field default appearance.
    /// </summary>
    public string FontName { get; set; } = StandardPdfFonts.Helvetica;

    /// <summary>
    /// Default text size used for the field appearance.
    /// </summary>
    public int FontSize { get; set; } = 12;

    /// <summary>
    /// Optional initial field value.
    /// </summary>
    public string? DefaultValue { get; set; }

    internal static TextFormFieldCreationOptions Initialize(Action<TextFormFieldCreationOptions>? configure)
    {
        var options = new TextFormFieldCreationOptions();
        configure?.Invoke(options);
        return options;
    }
}
