namespace ZingPDF.Fonts
{
    public interface IFontProvider
    {
        FontMetrics GetFontMetrics(string fontName);
        bool IsSupported(string fontName);
        float MeasureText(string text, string fontName, float fontSize);
    }
}