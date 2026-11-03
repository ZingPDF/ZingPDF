using ZingPDF.Extensions;
using ZingPDF.Elements.Drawing;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Text;

public class TextObject : ContentStream
{
    private static readonly System.Text.Encoding _winAnsi = CreateWinAnsiEncoding();
    internal IReadOnlyList<PdfFont> ReferencedFonts { get; private set; } = [];

    public TextObject(string text, Rectangle boundingBox, FontOptions fontOptions)
        : base(ObjectContext.UserCreated)
    {
        ArgumentNullException.ThrowIfNull(text, nameof(text));
        ArgumentNullException.ThrowIfNull(boundingBox, nameof(boundingBox));
        ArgumentNullException.ThrowIfNull(fontOptions, nameof(fontOptions));

        Build(text, boundingBox.LowerLeft, fontOptions, new(), boundingBox);
    }

    public TextObject(string text, Coordinate textOrigin, FontOptions fontOptions, Rectangle? clipBounds = null)
        : this(text, textOrigin, fontOptions, clipBounds, new TextPlacementOptions()) { }

    /// <summary>Creates baseline-positioned text with explicit line breaks and optional rotation.</summary>
    public TextObject(string text, Coordinate textOrigin, FontOptions fontOptions, Rectangle? clipBounds, TextPlacementOptions placement)
        : base(ObjectContext.UserCreated)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(textOrigin);
        ArgumentNullException.ThrowIfNull(fontOptions);
        ArgumentNullException.ThrowIfNull(placement);
        Build(text, textOrigin, fontOptions, placement, clipBounds);
    }

    private void Build(string text, Coordinate origin, FontOptions options, TextPlacementOptions placement, Rectangle? clip)
    {
        var size = options.Size.Value;
        var height = placement.LineHeight ?? size * 1.25;
        if (!double.IsFinite(size) || size <= 0) throw new ArgumentOutOfRangeException(nameof(options), "Font size must be positive and finite.");
        if (!double.IsFinite(height) || height <= 0 || !double.IsFinite(placement.RotationDegrees)) throw new ArgumentOutOfRangeException(nameof(placement));
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        // Encode before building operations, so missing glyphs cannot leave half-built content.
        var encoded = lines.Select(line => options.RegisteredFont?.Encode(line) ?? EncodeText(line, options.TextEncoding)).ToArray();
        var angle = placement.RotationDegrees * Math.PI / 180;
        var cos = Math.Cos(angle); var sin = Math.Sin(angle);
        if (Math.Abs(cos) < 1e-12) cos = 0; if (Math.Abs(sin) < 1e-12) sin = 0;
        this.SaveGraphicsState();
        if (clip is not null) this.SetClippingPath(clip);
        this.SetColour(options.Colour).BeginTextObject().SetTextState(options.ResourceName, options.Size);
        for (var index = 0; index < encoded.Length; index++)
        {
            var drop = height * index;
            this.SetTextMatrix(cos, sin, sin == 0 ? 0 : -sin, cos, origin.X + sin * drop, origin.Y - cos * drop).ShowText(encoded[index]);
        }
        this.EndTextObject().RestoreGraphicsState();
        if (options.RegisteredFont is { } font) ReferencedFonts = [font];
    }

    public TextObject(string text, Rectangle boundingBox, PdfFont font, Number size, Graphics.RGBColour colour)
        : this(text, boundingBox, font?.CreateOptions(size, colour) ?? throw new ArgumentNullException(nameof(font)))
    {
        ArgumentNullException.ThrowIfNull(font, nameof(font));
        ArgumentNullException.ThrowIfNull(colour, nameof(colour));
        ReferencedFonts = [font];
    }

    public TextObject(string text, Coordinate textOrigin, PdfFont font, Number size, Graphics.RGBColour colour, Rectangle? clipBounds = null)
        : this(text, textOrigin, font?.CreateOptions(size, colour) ?? throw new ArgumentNullException(nameof(font)), clipBounds)
    {
        ArgumentNullException.ThrowIfNull(font, nameof(font));
        ArgumentNullException.ThrowIfNull(colour, nameof(colour));
        ReferencedFonts = [font];
    }

    private static PdfString EncodeText(string text, FontTextEncoding encoding)
    {
        return encoding switch
        {
            FontTextEncoding.Auto => PdfString.FromTextAuto(text, ObjectContext.UserCreated),
            FontTextEncoding.WinAnsi => PdfString.FromBytes(_winAnsi.GetBytes(text), PdfStringSyntax.Literal, ObjectContext.UserCreated),
            _ => throw new InvalidOperationException($"Unsupported font text encoding '{encoding}'.")
        };
    }

    private static System.Text.Encoding CreateWinAnsiEncoding()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        return System.Text.Encoding.GetEncoding(
            1252,
            System.Text.EncoderFallback.ExceptionFallback,
            System.Text.DecoderFallback.ExceptionFallback);
    }
}
