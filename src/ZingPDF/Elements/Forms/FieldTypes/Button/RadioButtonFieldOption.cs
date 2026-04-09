using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// Defines a radio-button option to create within a radio-button field.
/// </summary>
public sealed class RadioButtonFieldOption
{
    public RadioButtonFieldOption(string value, Rectangle bounds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        ArgumentNullException.ThrowIfNull(bounds, nameof(bounds));

        Value = value;
        Bounds = bounds;
    }

    /// <summary>
    /// Gets the exported value for this radio-button option.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the widget bounds for this option.
    /// </summary>
    public Rectangle Bounds { get; }
}
