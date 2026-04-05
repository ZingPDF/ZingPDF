using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Graphics.Images
{
    internal class ImageXObjectContentStream : ContentStream
    {
        public ImageXObjectContentStream(Name name, Rectangle maxBounds, ObjectContext context)
            : base(context)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(maxBounds);

            this.SaveGraphicsState()
                .ConcatenateMatrix(1, 0, 0, 1, maxBounds.LowerLeft.X, maxBounds.LowerLeft.Y)
                .ConcatenateMatrix(maxBounds.Width, 0, 0, maxBounds.Height, 0, 0)
                .InvokeXObject(name)
                .RestoreGraphicsState();
        }
    }
}
