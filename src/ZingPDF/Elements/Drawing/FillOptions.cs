using ZingPDF.Graphics;

namespace ZingPDF.Elements.Drawing
{
    public class FillOptions(RGBColour colour)
    {
        /// <summary>
        /// Fill colour.
        /// </summary>
        public RGBColour Colour { get; } = colour ?? throw new ArgumentNullException(nameof(colour));
    }
}
