using ZingPDF.Graphics;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Text;

public record FontOptions
{
    /// <summary>
    /// The name of the font resource.
    /// </summary>
    public required Name ResourceName { get; init; }

    /// <summary>
    /// The size of the font.
    /// </summary>
    public required Number Size { get; init; }
    
    /// <summary>
    /// The colour of the font.
    /// </summary>
    public required RGBColour Colour { get; init; }

    /// <summary>
    /// Controls how text should be encoded for the font resource.
    /// </summary>
    public FontTextEncoding TextEncoding { get; init; } = FontTextEncoding.Auto;
}
