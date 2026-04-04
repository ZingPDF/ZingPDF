using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Graphics.FormXObjects;

internal sealed class FormXObjectContentStream : ContentStream
{
    public FormXObjectContentStream(Name name, Rectangle targetBounds, Rectangle sourceBounds, ObjectContext context)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(targetBounds);
        ArgumentNullException.ThrowIfNull(sourceBounds);

        var sourceWidth = (double)sourceBounds.Width;
        var sourceHeight = (double)sourceBounds.Height;
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return;
        }

        var scaleX = (double)targetBounds.Width / sourceWidth;
        var scaleY = (double)targetBounds.Height / sourceHeight;
        var translateX = (double)targetBounds.LowerLeft.X - ((double)sourceBounds.LowerLeft.X * scaleX);
        var translateY = (double)targetBounds.LowerLeft.Y - ((double)sourceBounds.LowerLeft.Y * scaleY);

        this.SaveGraphicsState()
            .ConcatenateMatrix(scaleX, 0, 0, scaleY, translateX, translateY)
            .InvokeXObject(name)
            .RestoreGraphicsState();
    }
}
