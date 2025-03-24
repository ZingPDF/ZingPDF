using ZingPDF.Elements.Drawing;
using ZingPDF.Elements.Drawing.Text;
using ZingPDF.Graphics;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Strings;
using static ZingPDF.Syntax.ContentStreamsAndResources.ContentStream.Operators;

namespace ZingPDF.Syntax.ContentStreamsAndResources;

public static class ContentStreamExtensions
{
    public static ContentStream SaveGraphicsState(this ContentStream contentStream)
    {
        contentStream.Operations.Add(new ContentStreamOperation { Operator = GeneralGraphicsState.q });

        return contentStream;
    }

    public static ContentStream RestoreGraphicsState(this ContentStream contentStream)
    {
        contentStream.Operations.Add(new ContentStreamOperation { Operator = GeneralGraphicsState.Q });

        return contentStream;
    }

    public static ContentStream BeginMarkedContentRegion(this ContentStream stream, Name tag)
    {
        stream.Operations.Add(new ContentStreamOperation { Operator = MarkedContent.BMC, Operands = [tag] });

        return stream;
    }

    public static ContentStream EndMarkedContentRegion(this ContentStream stream)
    {
        stream.Operations.Add(new ContentStreamOperation { Operator = MarkedContent.EMC });

        return stream;
    }

    public static ContentStream SetClippingPath(this ContentStream stream, Rectangle boundingBox)
    {
        // Append rectangle to path
        stream.Operations.Add(new ContentStreamOperation
        {
            Operator = PathConstruction.re,
            Operands = [
                    boundingBox.LowerLeft.X,
                    boundingBox.LowerLeft.Y,
                    boundingBox.UpperRight.X,
                    boundingBox.UpperRight.Y
                    ]
        });

        // Set clipping path
        stream.Operations.Add(new ContentStreamOperation { Operator = ClippingPaths.W });

        // End path
        stream.Operations.Add(new ContentStreamOperation { Operator = PathPainting.n });

        return stream;
    }

    public static ContentStream SetColour(this ContentStream stream, RGBColour colour)
    {
        stream.Operations.Add(new ContentStreamOperation
        {
            Operator = Colour.rg,
            Operands = [.. colour.Values]
        });

        return stream;
    }

    public static ContentStream BeginTextObject(this ContentStream stream)
    {
        stream.Operations.Add(new ContentStreamOperation { Operator = TextObjects.BT });

        return stream;
    }

    public static ContentStream EndTextObject(this ContentStream stream)
    {
        stream.Operations.Add(new ContentStreamOperation { Operator = TextObjects.ET });

        return stream;
    }

    public static ContentStream ShowText(this ContentStream stream, LiteralString text)
    {
        stream.Operations.Add(new ContentStreamOperation { Operator = TextShowing.Tj, Operands = [text] });

        return stream;
    }

    public static ContentStream SetTextMatrix(this ContentStream stream, params Number[] matrix)
    {
        stream.Operations.Add(new ContentStreamOperation
        {
            Operator = TextPositioning.Tm,
            Operands = [.. matrix]
        });

        return stream;
    }

    public static ContentStream SetTextPosition(this ContentStream stream, Coordinate position)
    {
        stream.Operations.Add(new ContentStreamOperation { Operator = TextPositioning.Td, Operands = [position.X, position.Y] });

        return stream;
    }

    public static ContentStream SetTextState(this ContentStream stream, Name fontResource, Number fontSize)
    {
        stream.Operations.Add(new ContentStreamOperation { Operator = TextState.Tf, Operands = [fontResource, fontSize] });

        return stream;
    }

    public static ContentStream AddOperations(this ContentStream stream, IEnumerable<ContentStreamOperation> operations)
    {
        stream.Operations.AddRange(operations);

        return stream;
    }

    /// <summary>
    /// The <paramref name="contentStream"/> parameter gives access to the content stream between the operations found by the given 
    /// predicates <paramref name="first"/> and <paramref name="last"/>. All existing operations between these operations will be deleted.
    /// </summary>
    public static async Task<ContentStream> ClearAndOperateBetweenAsync(
        this ContentStream stream,
        Predicate<ContentStreamOperation> first,
        Predicate<ContentStreamOperation> last,
        Func<ContentStream, Task> contentStream
        )
    {
        var index1 = stream.Operations.FindIndex(first);
        if (index1 == -1)
        {
            throw new InvalidOperationException();
        }

        var index2 = stream.Operations.FindIndex(last);
        if (index2 == -1)
        {
            throw new InvalidOperationException();
        }

        var newContentStream = new ContentStream();

        newContentStream.AddOperations(stream.Operations.Take(index1 + 1));

        await contentStream(newContentStream);

        newContentStream.AddOperations(stream.Operations.Skip(index2));

        stream.Operations.Clear();
        stream.Operations.AddRange(newContentStream.Operations);

        return stream;
    }

    /// <summary>
    /// Writes a marked content region for variable text, as accepted by Adobe Acrobat.
    /// </summary>
    public static async Task<ContentStream> WriteTextContentRegionAsync(this ContentStream stream, Func<ContentStream, Task> contentStream)
    {
        stream.BeginMarkedContentRegion(Constants.Acrobat.MarkedContent.Tx);

        await contentStream(stream);

        stream.EndMarkedContentRegion();

        return stream;
    }
}