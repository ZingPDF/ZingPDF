using ZingPDF.Elements.Drawing;
using ZingPDF.Extensions;
using ZingPDF.Graphics;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Text;

public class TextObject : ContentStreamObject
{
    private readonly LiteralString _text;
    private readonly Rectangle _boundingBox;
    private readonly Coordinate _textOrigin;
    private readonly FontOptions _fontOptions;

    public TextObject(LiteralString text, Rectangle boundingBox, Coordinate textOrigin, FontOptions fontOptions)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _boundingBox = boundingBox ?? throw new ArgumentNullException(nameof(boundingBox));
        _textOrigin = textOrigin ?? throw new ArgumentNullException(nameof(textOrigin));
        _fontOptions = fontOptions ?? throw new ArgumentNullException(nameof(fontOptions));
    }

    protected override async Task WriteOutputAsync(Stream stream)
    {
        // Save graphics state
        await stream.WriteTextAsync(Operators.GeneralGraphicsState.q);
        await stream.WriteWhitespaceAsync();

        // Add clipping path
        await stream.WriteDoubleAsync(_boundingBox.LowerLeft.X);
        await stream.WriteWhitespaceAsync();

        await stream.WriteDoubleAsync(_boundingBox.LowerLeft.Y);
        await stream.WriteWhitespaceAsync();

        await stream.WriteDoubleAsync(_boundingBox.UpperRight.X);
        await stream.WriteWhitespaceAsync();

        await stream.WriteDoubleAsync(_boundingBox.UpperRight.Y);
        await stream.WriteWhitespaceAsync();

        await stream.WriteTextAsync(Operators.PathConstruction.re);
        await stream.WriteWhitespaceAsync();

        await stream.WriteTextAsync(Operators.ClippingPaths.W);
        await stream.WriteWhitespaceAsync();

        await stream.WriteTextAsync(Operators.PathPainting.n);
        await stream.WriteWhitespaceAsync();

        // Begin text object
        await stream.WriteTextAsync(Operators.TextObjects.BT);
        await stream.WriteWhitespaceAsync();

        // Set text font and size
        // /F1 12 Tf
        await _fontOptions.FontResource.WriteAsync(stream);
        await stream.WriteWhitespaceAsync();
        await _fontOptions.Size.WriteAsync(stream);
        await stream.WriteWhitespaceAsync();
        await stream.WriteTextAsync(Operators.TextState.Tf);
        await stream.WriteWhitespaceAsync();

        // Set colour
        // 0.5 0.5 0.5 rg
        await _fontOptions.Colour.WriteAsync(stream);
        await stream.WriteTextAsync(Operators.Colour.rg);
        await stream.WriteWhitespaceAsync();

        // Position text
        // 2 5 Td
        await stream.WriteIntAsync(_textOrigin.X);
        await stream.WriteWhitespaceAsync();
        await stream.WriteIntAsync(_textOrigin.Y);
        await stream.WriteWhitespaceAsync();
        await stream.WriteTextAsync(Operators.TextPositioning.Td);
        await stream.WriteWhitespaceAsync();

        // Show text
        // (text) Tj
        await _text.WriteAsync(stream);
        await stream.WriteWhitespaceAsync();
        await stream.WriteTextAsync(Operators.TextShowing.Tj);
        await stream.WriteWhitespaceAsync();

        // End text object
        await stream.WriteTextAsync(Operators.TextObjects.ET);
        await stream.WriteWhitespaceAsync();

        // Restore graphics state
        await stream.WriteTextAsync(Operators.GeneralGraphicsState.Q);
    }

    public class FontOptions(Name fontResource, Integer size, RGBColour colour)
    {
        public Name FontResource { get; } = fontResource ?? throw new ArgumentNullException(nameof(fontResource));
        public Integer Size { get; } = size ?? throw new ArgumentNullException(nameof(size));
        public RGBColour Colour { get; } = colour ?? throw new ArgumentNullException(nameof(colour));
    }
}
