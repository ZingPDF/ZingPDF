namespace ZingPDF.Drawing
{
    public class FillOptions
    {
        public FillOptions(RGBAColour colour)
        {
            Colour = colour ?? throw new ArgumentNullException(nameof(colour));
        }

        /// <summary>
        /// Fill colour.
        /// </summary>
        public RGBAColour Colour { get; } = RGBAColour.Black;
    }
}
