using ZingPDF.Graphics;

namespace ZingPDF.Elements.Drawing
{
    public class FillOptions
    {
        public FillOptions(RGBColour colour)
        {
            Colour = colour ?? throw new ArgumentNullException(nameof(colour));
        }

        /// <summary>
        /// Fill colour.
        /// </summary>
        public RGBColour Colour { get; } = RGBColour.Black;
    }
}
