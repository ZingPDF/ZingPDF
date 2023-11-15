namespace ZingPdf.Core.Drawing
{
    public class BoundingBox
    {
        public BoundingBox(Point origin, int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), "Argument must be greater than zero");
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height), "Argument must be greater than zero");

            Origin = origin ?? throw new ArgumentNullException(nameof(origin));
            Width = width;
            Height = height;
        }

        /// <summary>
        /// The origin at which the bounds are located.
        /// </summary>
        public Point Origin { get; }

        /// <summary>
        /// The width of the bounding box.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the bounding box.
        /// </summary>
        public int Height { get; }
    }
}
