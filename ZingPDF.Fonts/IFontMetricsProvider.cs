namespace ZingPDF.Fonts
{
    public interface IFontMetricsProvider
    {
        FontMetrics GetFontMetrics(string fontName);
        bool IsSupported(string fontName);
        double MeasureText(string text, string fontName, double fontSize);
    }
}