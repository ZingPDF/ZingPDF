using SkiaSharp;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Syntax.CommonDataStructures;
using static ZingPDF.Constants.DictionaryKeys;

namespace ZingPDF.Elements.Drawing.Text;

internal class TextCalculations : ITextCalculations
{
    private readonly IEnumerable<IFontProvider> _fontProviders;

    public TextCalculations(IEnumerable<IFontProvider> fontProviders)
    {
        _fontProviders = fontProviders;
    }
    //    //public FontFit CalculateFontSize(FontMetrics fontMetrics, Rectangle boundingBox, string text)
    //    //{
    //    //    const float minFontSize = 4.0f;
    //    //    const float targetFontToBoxHeightRatio = 2 / 3f;

    //    //    float totalFontHeight = fontMetrics.Ascent - fontMetrics.Descent;

    //    //    // Calculate the initial font size based on the height of the bounding box
    //    //    var fontSize = (float)boundingBox.Height * targetFontToBoxHeightRatio * 1000 / totalFontHeight;

    //    //    // Measure the width of the text at the calculated font size
    //    //    float textWidth = MeasureTextWidth(fontMetrics, text, fontSize);

    //    //    // If the text width overflows the bounding box, reduce the font size
    //    //    while (textWidth > boundingBox.Width && fontSize > minFontSize)
    //    //    {
    //    //        fontSize -= 1.0f; // Decrease the font size by 1 point
    //    //        textWidth = MeasureTextWidth(fontMetrics, text, fontSize);
    //    //    }

    //    //    // Ensure the font size does not go below the minimum font size
    //    //    fontSize = Math.Max(fontSize, minFontSize);

    //    //    // Calculate the total height of the font at the calculated size
    //    //    float scaledTotalFontHeight = totalFontHeight * fontSize / 1000f;

    //    //    // Calculate the baseline offset from the bottom of the bounding box
    //    //    float baselineOffset = ((float)boundingBox.Height - scaledTotalFontHeight) / 2;

    //    //    return new FontFit
    //    //    {
    //    //        FontSize = fontSize,
    //    //        Baseline = baselineOffset
    //    //    };
    //    //}

    public TextFit CalculateTextFit(string fontName, Rectangle boundingBox, string text)
    {
        IFontProvider fontProvider = _fontProviders.FirstOrDefault(x => x.IsSupported(fontName))
            ?? throw new InvalidOperationException($"Font '{fontName}' is not supported");

        FontMetrics fontMetrics = fontProvider.GetFontMetrics(fontName);

        const float minFontSize = 4.0f;
        const float targetFontToBoxHeightRatio = 2 / 3f;
        const float opticalVerticalCenteringFactor = 0.677f;

        float totalFontHeight = fontMetrics.Ascent - fontMetrics.Descent;

        // Calculate the initial font size based on the height of the bounding box
        var fontSize = (float)boundingBox.Height * targetFontToBoxHeightRatio * 1000 / totalFontHeight;

        // Measure the width of the text at the calculated font size
        float textWidth = fontProvider.MeasureText(text, fontName, fontSize);

        // Calculate a bounding box which is 2 pixels smaller on all sides to account for padding
        var paddedBoundingBox = Rectangle.FromDimensions(boundingBox.Width - 4, boundingBox.Height - 4);

        // If the text width overflows the padded bounding box, reduce the font size
        while (textWidth > paddedBoundingBox.Width && fontSize > minFontSize)
        {
            fontSize -= 1.0f; // Decrease the font size by 1 point
            textWidth = fontProvider.MeasureText(text, fontName, fontSize);
        }

        // Ensure the font size does not go below the minimum font size
        fontSize = Math.Max(fontSize, minFontSize);

        // Optically centre the text vertically
        // Step 1: Scale font metrics according to font size
        //float scaledAscent = (fontMetrics.Ascent / 1000f) * fontSize;
        //float scaledDescent = (fontMetrics.Descent / 1000f) * fontSize;

        // Step 2: Calculate the geometric baseline
        //float geometricBaseline = ((float)boundingBox.Height + scaledAscent + scaledDescent) / 2;

        // Step 3: Total text height is the distance between ascender and descender
        //float scaledTotalFontHeight = scaledAscent - scaledDescent;

        // Step 4: Compute optical adjustment
        //float opticalAdjustment = opticalVerticalCenteringFactor * scaledTotalFontHeight;

        // Step 5: Optical (visually centered) baseline position from the bottom
        //float opticalBaseline = geometricBaseline - opticalAdjustment;

        float fieldVerticalCenter = boundingBox.Height / 2;
        float halfScaledXHeight = fontMetrics.XHeight * fontSize / 2000;
        float strokeWidthOpticalAdjustment = fontMetrics.StandardVerticalWidth * fontSize * 1.25f / 1000;

        float opticalBaseline = fieldVerticalCenter - halfScaledXHeight - strokeWidthOpticalAdjustment;

        return new TextFit
        {
            FontSize = fontSize,
            TextOrigin = new Coordinate(2, opticalBaseline) // TODO: This is left-aligned only, account for quadding
        };
    }

    //    public float MeasureTextWidth(string fontName, string text, float fontSize)
    //    {
    //        var typeface = SKTypeface.FromFamilyName(fontName);
    //        //var typeface = SKTypeface.FromData(SKData.Create(fontData));

    //        var font = new SKFont(typeface, fontSize);

    //        //var fontMetrics = font.GetFontMetrics();

    //        return font.MeasureText(text);
    //    }

    //    //public float MeasureTextWidth(FontMetrics fontMetrics, string text, float fontSize)
    //    //{
    //    //    float totalWidth = 0.0f;

    //    //    foreach (char c in text)
    //    //    {
    //    //        if (fontMetrics.Widths.TryGetValue(c, out int charWidth))
    //    //        {
    //    //            totalWidth += charWidth;
    //    //        }
    //    //        else
    //    //        {
    //    //            totalWidth += fontMetrics.DefaultWidth;
    //    //        }
    //    //    }

    //    //    // Scale the total width by the font size
    //    //    return totalWidth * fontSize / 1000.0f;
    //    //}

    //    //public (float fontSize, float baselineOffset) FitTextInBox(Stream fontData, float boxHeight)
    //    //{
    //    //    var fontMetrics = new SKFont(SKTypeface.FromData(SKData.Create(fontData))).GetFontMetrics();

    //    //    return FitTextInBox(fontMetrics, boxHeight);
    //    //}

    //    //public (float fontSize, float baselineOffset) FitTextInBox(string fontName, float boxHeight)
    //    //{
    //    //    if (!StandardPdfFonts.Metrics.TryGetValue(fontName, out var metrics))
    //    //    {
    //    //        throw new InvalidOperationException("This overload can only be used for the 14 standard PDF fonts");
    //    //    }

    //    //    return FitTextInBox(metrics, boxHeight);
    //    //}

    //    //public (float fontSize, float baselineOffset) FitTextInBox(FontMetrics fontMetrics, float boxHeight)
    //    //{
    //    //    float totalHeight = -fontMetrics.Ascent + fontMetrics.Descent + fontMetrics.Leading;

    //    //    // Scale font size so the full text height fits in the box
    //    //    float fontSize = boxHeight / totalHeight;

    //    //    // Calculate baseline position (from top of the box)
    //    //    float baselineOffset = -fontMetrics.Ascent / totalHeight * boxHeight;

    //    //    return (fontSize, baselineOffset);
    //    //}
}
