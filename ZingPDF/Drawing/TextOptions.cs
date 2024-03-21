namespace ZingPDF.Drawing
{
    public class TextOptions
    {
        public TextOptions(string fontFamily, int fontSize, RGBAColour colour)
        {
            if (string.IsNullOrWhiteSpace(fontFamily)) throw new ArgumentException($"'{nameof(fontFamily)}' cannot be null or whitespace.", nameof(fontFamily));
            if (fontSize < 1) throw new ArgumentOutOfRangeException(nameof(fontSize), $"{nameof(fontSize)} must be greater than zero");

            FontFamily = fontFamily;
            FontSize = fontSize;
            Colour = colour ?? throw new ArgumentNullException(nameof(colour));
        }

        public string FontFamily { get; }
        public int FontSize { get; }
        public RGBAColour Colour { get; } = RGBAColour.Black;
    }
}
