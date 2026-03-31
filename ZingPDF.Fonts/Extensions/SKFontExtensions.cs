using SkiaSharp;

namespace ZingPDF.Fonts.Extensions;

internal static class SKFontExtensions
{
    public static FontMetrics GetFontMetrics(this SKFont font)
    {
        return new FontMetrics
        {
            Name = font.Typeface.FamilyName,
            // Convert from Skia's scale to our 1000-unit scale
            Ascent = ConvertToEmUnits(Math.Abs(font.Metrics.Ascent), font),
            Descent = ConvertToEmUnits(-font.Metrics.Descent, font), // Negate to match our convention
            CapHeight = ConvertToEmUnits(font.Metrics.CapHeight, font),
            XHeight = ConvertToEmUnits(font.Metrics.XHeight, font),
            ItalicAngle = font.SkewX,
            IsFixedPitch = font.Typeface.IsFixedPitch,
            UnderlinePosition = font.Metrics.UnderlinePosition.HasValue ? ConvertToEmUnits(font.Metrics.UnderlinePosition.Value, font) : null,
            UnderlineThickness = font.Metrics.UnderlineThickness.HasValue ? ConvertToEmUnits(font.Metrics.UnderlineThickness.Value, font) : null,
        };
    }

    public static FontBoundingBox GetBoundingBox(this SKFont font)
    {
        var left = ConvertToEmUnits(font.Metrics.XMin, font);
        var right = ConvertToEmUnits(font.Metrics.XMax, font);
        // Skia metrics use a Y-down coordinate system. PDF font metrics use Y-up.
        var top = ConvertToEmUnits(-font.Metrics.Top, font);
        var bottom = ConvertToEmUnits(-font.Metrics.Bottom, font);

        return new FontBoundingBox(
            Math.Min(left, right),
            Math.Min(bottom, top),
            Math.Max(left, right),
            Math.Max(bottom, top));
    }

    private static int ConvertToEmUnits(float skiaValue, SKFont skFont)
    {
        // SkiaSharp uses pixels at the current font size, so we need to scale
        // back to the 1000-unit em square
        float emScale = 1000f / skFont.Size;
        return (int)Math.Round(skiaValue * emScale);
    }
}
