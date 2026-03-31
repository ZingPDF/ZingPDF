using System.Text;
using ZingPDF.Graphics;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Text;

/// <summary>
/// A font registered with a PDF document and ready to use from page APIs.
/// </summary>
public sealed class PdfFont
{
    internal PdfFont(
        Name resourceName,
        IndirectObjectReference fontReference,
        string baseFontName,
        FontTextEncoding textEncoding,
        bool isEmbedded)
    {
        ResourceName = resourceName ?? throw new ArgumentNullException(nameof(resourceName));
        FontReference = fontReference ?? throw new ArgumentNullException(nameof(fontReference));
        BaseFontName = baseFontName ?? throw new ArgumentNullException(nameof(baseFontName));
        TextEncoding = textEncoding;
        IsEmbedded = isEmbedded;
    }

    /// <summary>
    /// The resource name used to reference the font inside page content streams.
    /// </summary>
    public Name ResourceName { get; }

    /// <summary>
    /// The PDF base font name.
    /// </summary>
    public string BaseFontName { get; }

    /// <summary>
    /// Gets whether the font is embedded in the document.
    /// </summary>
    public bool IsEmbedded { get; }

    internal IndirectObjectReference FontReference { get; }
    internal FontTextEncoding TextEncoding { get; }

    /// <summary>
    /// Creates font options for use with higher-level text APIs.
    /// </summary>
    public FontOptions CreateOptions(Number size, RGBColour colour)
    {
        return new FontOptions
        {
            ResourceName = ResourceName,
            Size = size,
            Colour = colour,
            TextEncoding = TextEncoding
        };
    }
}
