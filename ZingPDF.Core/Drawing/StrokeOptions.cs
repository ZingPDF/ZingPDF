namespace ZingPdf.Core.Drawing
{
    public class StrokeOptions
    {
        public StrokeOptions(RGBAColour colour, int width)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), "Value must be greater than zero");

            Colour = colour ?? throw new ArgumentNullException(nameof(colour));
            Width = width;
        }

        /// <summary>
        /// Stroke colour.
        /// </summary>
        public RGBAColour Colour { get; } = RGBAColour.Black;

        /// <summary>
        /// Stroke width in pixels.
        /// </summary>
        public int Width { get; } = 1;
    }
}
