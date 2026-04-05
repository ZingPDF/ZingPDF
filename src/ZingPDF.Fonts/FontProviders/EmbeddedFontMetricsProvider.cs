using SkiaSharp;
using ZingPDF.Fonts.Extensions;

namespace ZingPDF.Fonts.FontProviders;

/// <summary>
/// Font metrics provider that uses embedded font data
/// </summary>
public class EmbeddedFontMetricsProvider : IFontMetricsProvider
{
    private readonly Dictionary<string, Stream> _embeddedFontData = [];

    public EmbeddedFontMetricsProvider(Dictionary<string, Stream> embeddedFontData)
    {
        _embeddedFontData = embeddedFontData;
    }

    public FontMetrics GetFontMetrics(string fontName)
    {
        if (!_embeddedFontData.TryGetValue(fontName, out Stream? fontData))
        {
            throw new FontNotFoundException($"Font '{fontName}' not found.");
        }

        //fontData.Position = 0;

        var typeface = SKTypeface.FromData(SKData.Create(fontData));

        return new SKFont(typeface).GetFontMetrics();
    }

    public bool IsSupported(string fontName) => _embeddedFontData.ContainsKey(fontName);

    public double MeasureText(string text, string fontName, double fontSize)
    {
        if (!_embeddedFontData.TryGetValue(fontName, out Stream? fontData))
        {
            throw new FontNotFoundException($"Font '{fontName}' not found.");
        }

        var typeface = SKTypeface.FromData(SKData.Create(fontData));

        return new SKFont(typeface, (float)fontSize).MeasureText(text);
    }
}
