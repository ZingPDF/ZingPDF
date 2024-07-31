using ZingPDF.Graphics;

namespace ZingPDF.Elements.Drawing
{
    public class StrokeOptions
    {
        public StrokeOptions(RGBColour colour, int width)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), "Value must be greater than zero");

            Colour = colour ?? throw new ArgumentNullException(nameof(colour));
            Width = width;
        }

        /// <summary>
        /// Stroke colour.
        /// </summary>
        public RGBColour Colour { get; } = RGBColour.Black;

        /// <summary>
        /// Stroke width in pixels.
        /// </summary>
        public int Width { get; } = 1;
    }
}
