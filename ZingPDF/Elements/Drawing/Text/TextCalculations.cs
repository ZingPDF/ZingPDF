using SkiaSharp;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Syntax.CommonDataStructures;
using static ZingPDF.Constants.DictionaryKeys;

namespace ZingPDF.Elements.Drawing.Text;

internal class TextCalculations : ITextCalculations
{
    // This is a lower limit for the font size to prevent the text from becoming unreadable
    // This is consistent with the observed lower limit in Adobe Acrobat Reader
    private const double _minFontSize = 4.0;

    //// This will be used to compute a font size as a pecentage of the usable field height (that is, the height minus padding)
    //private const double _initialFontToBoxHeightRatio = 0.95;

    // This will be used to compute a font size as a pecentage of the field height
    private const double _initialFontToBoxHeightRatio = 0.89;

    private const double _opticalBaselineAdjustment = 0.709;

    private readonly IEnumerable<IFontMetricsProvider> _fontProviders;

    public TextCalculations(IEnumerable<IFontMetricsProvider> fontProviders)
    {
        _fontProviders = fontProviders;
    }

    public TextFit CalculateTextFit(string fontName, Rectangle boundingBox, string text)
    {
        IFontMetricsProvider fontProvider = _fontProviders.FirstOrDefault(x => x.IsSupported(fontName))
            ?? throw new InvalidOperationException($"Font '{fontName}' is not supported");

        FontMetrics fontMetrics = fontProvider.GetFontMetrics(fontName);

        // Calculate a bounding box which is 2 pixels smaller on all sides to account for padding
        var paddedBoundingBox = Rectangle.FromDimensions(
            boundingBox.Width - 4,
            boundingBox.Height - 4
            );

        int totalFontHeight = fontMetrics.Ascent - fontMetrics.Descent;

        // Step 1: Derive an initial font size based on the height of the bounding box
        //double fontSize = paddedBoundingBox.Height * (1000 / totalFontHeight) * 0.685;
        double fontSize = 1.294 * Math.Pow(boundingBox.Height, 0.7887);
        //double fontSize = 2.552 * Math.Pow(paddedBoundingBox.Height, 0.609);

        // Step 2: Measure the width of the text at the calculated font size
        double textWidth = fontProvider.MeasureText(text, fontName, fontSize);

        // If the text width overflows the padded bounding box, reduce the font size
        while (textWidth > paddedBoundingBox.Width && fontSize > _minFontSize)
        {
            fontSize -= 0.1d; // Decrease the font size by 0.1 points
            textWidth = fontProvider.MeasureText(text, fontName, fontSize);
        }

        // Ensure the font size does not go below the minimum font size
        fontSize = Math.Max(fontSize, _minFontSize);

        double scaledXHeight = (fontMetrics.XHeight / 1000d) * fontSize;
        double halfFieldHeight = boundingBox.Height / 2;
        double opticalBaseline = halfFieldHeight - (_opticalBaselineAdjustment * scaledXHeight);

        return new TextFit
        {
            FontSize = fontSize,
            TextOrigin = new Coordinate(2, opticalBaseline) // TODO: This is left-aligned only, account for quadding
        };
    }
}
