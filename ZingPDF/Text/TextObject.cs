using ZingPDF.Extensions;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Text;

public class TextObject : ContentStream
{
    // TODO: should the position of the text box be controlled outside this object?
    //  text object should be text, font, size, box size
    public TextObject(string text, Rectangle boundingBox, FontOptions fontOptions)
        : base(ObjectOrigin.UserCreated)
    {
        ArgumentNullException.ThrowIfNull(text, nameof(text));
        ArgumentNullException.ThrowIfNull(fontOptions, nameof(fontOptions));

        // Replace EOL characters with T* operators
        // TODO: test this
        text = text.Replace(new string(Constants.EndOfLineCharacters), $") {Operators.TextPositioning.TStar} (");

        // TODO: test text box size and text position etc
        this
            .SaveGraphicsState()
            .SetClippingPath(boundingBox.Size)
            .SetTextState(fontOptions.ResourceName, fontOptions.Size)
            .SetColour(fontOptions.Colour)
            .BeginTextObject()
            .SetTextPosition(boundingBox.LowerLeft)
            .ShowText(text)
            .EndTextObject()
            .RestoreGraphicsState();
    }
}
