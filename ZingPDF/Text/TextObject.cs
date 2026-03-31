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

    // TODO: should the position of the text box be controlled outside this object?
    //  text object should be text, font, size, box size
    public TextObject(string text, Rectangle boundingBox, FontOptions fontOptions)
        : base(ObjectContext.UserCreated)
    {
        ArgumentNullException.ThrowIfNull(text, nameof(text));
        ArgumentNullException.ThrowIfNull(boundingBox, nameof(boundingBox));
        ArgumentNullException.ThrowIfNull(fontOptions, nameof(fontOptions));

        this
            .SaveGraphicsState()
            .SetClippingPath(boundingBox)
            .SetTextState(fontOptions.ResourceName, fontOptions.Size)
            .SetColour(fontOptions.Colour)
            .BeginTextObject()
            .SetTextPosition(boundingBox.LowerLeft)
            .ShowText(EncodeText(text, fontOptions.TextEncoding))
            .EndTextObject()
            .RestoreGraphicsState();
    }

    public TextObject(string text, Coordinate textOrigin, FontOptions fontOptions, Rectangle? clipBounds = null)
        : base(ObjectContext.UserCreated)
    {
        ArgumentNullException.ThrowIfNull(text, nameof(text));
        ArgumentNullException.ThrowIfNull(textOrigin, nameof(textOrigin));
        ArgumentNullException.ThrowIfNull(fontOptions, nameof(fontOptions));

        this.SaveGraphicsState();

        if (clipBounds is not null)
        {
            this.SetClippingPath(clipBounds);
        }

        this
            .SetTextState(fontOptions.ResourceName, fontOptions.Size)
            .SetColour(fontOptions.Colour)
            .BeginTextObject()
            .SetTextPosition(textOrigin)
            .ShowText(EncodeText(text, fontOptions.TextEncoding))
            .EndTextObject()
            .RestoreGraphicsState();
    }

    public TextObject(string text, Rectangle boundingBox, PdfFont font, Number size, Graphics.RGBColour colour)
        : this(text, boundingBox, font.CreateOptions(size, colour))
    {
        ArgumentNullException.ThrowIfNull(font, nameof(font));
        ArgumentNullException.ThrowIfNull(colour, nameof(colour));
    }

    private static PdfString EncodeText(string text, FontTextEncoding encoding)
    {
        // Replace EOL characters with T* operators
        // TODO: test this
        text = text.Replace(new string(Constants.EndOfLineCharacters), $") {Operators.TextPositioning.TStar} (");

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
