namespace ZingPDF.Fonts.FontProviders
{
    public class SimpleFontMetricsProvider : IFontMetricsProvider
    {
        public Dictionary<string, FontMetrics> FontMetrics { get; } = [];

        public FontMetrics GetFontMetrics(string fontName)
        {
            return FontMetrics[fontName];
        }

        public bool IsSupported(string fontName)
        {
            return FontMetrics.ContainsKey(fontName);
        }

        public double MeasureText(string text, string fontName, double fontSize)
        {
            return GetFontMetrics(fontName).CalculateStringWidth(text, fontSize);
        }
    }
}
