using SkiaSharp;
namespace ZingPDF.Fonts.FontProviders;

public class StreamFontProvider : IFontProvider
{
    private readonly Dictionary<string, Stream> _embeddedFontData = [];

    public StreamFontProvider(Dictionary<string, Stream> embeddedFontData)
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

    public float MeasureText(string text, string fontName, float fontSize)
    {
        if (!_embeddedFontData.TryGetValue(fontName, out Stream? fontData))
        {
            throw new FontNotFoundException($"Font '{fontName}' not found.");
        }

        var typeface = SKTypeface.FromData(SKData.Create(fontData));

        return new SKFont(typeface, fontSize).MeasureText(text);
    }
}
