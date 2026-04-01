using System.Text.RegularExpressions;
using ZingPDF.Fonts;
using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Elements.Drawing.Text;

internal class TextCalculations : ITextCalculations
{
    // This is a lower limit for the font size to prevent the text from becoming unreadable.
    // This is consistent with the observed lower limit in Adobe Acrobat Reader.
    private const double _minFontSize = 4.0;
    private const double _horizontalInset = 2.0;
    private const double _clipInset = 1.0;
    private const double _multilineTopInset = 2.0;
    private const double _lineHeightMultiplier = 1.2;

    // Calibrated against Acrobat-generated auto-sized field appearances.
    private const double _autoFontSizeToUsableHeightRatio = 0.741;

    private const double _opticalBaselineAdjustment = 0.709;
    private const double _capHeightToXHeightFallbackRatio = 0.75;
    private const double _ascentToXHeightFallbackRatio = 0.73;

    private static readonly Regex _lineTokenRegex = new(@"\S+\s*", RegexOptions.Compiled);

    private readonly IEnumerable<IFontMetricsProvider> _fontProviders;

    public TextCalculations(IEnumerable<IFontMetricsProvider> fontProviders)
    {
        _fontProviders = fontProviders;
    }

    public TextFit CalculateTextFit(string fontName, Rectangle boundingBox, string text, TextFitOptions? options = null)
    {
        var layout = CalculateTextLayout(fontName, boundingBox, text, options);
        var firstSegment = layout.Segments.FirstOrDefault()
            ?? new TextLayoutSegment
            {
                Text = string.Empty,
                Origin = new Coordinate(
                    (double)boundingBox.LowerLeft.X + _horizontalInset,
                    (double)boundingBox.LowerLeft.Y + ((double)boundingBox.Height / 2d))
            };

        return new TextFit
        {
            FontSize = layout.FontSize,
            TextOrigin = firstSegment.Origin
        };
    }

    public TextLayout CalculateTextLayout(string fontName, Rectangle boundingBox, string text, TextFitOptions? options = null)
    {
        options ??= new TextFitOptions();

        var fontProvider = _fontProviders.FirstOrDefault(x => x.IsSupported(fontName))
            ?? throw new InvalidOperationException($"Font '{fontName}' is not supported");
        var fontMetrics = fontProvider.GetFontMetrics(fontName);

        if (options.IsComb && options.MaxLength is > 0)
        {
            return CalculateCombLayout(fontName, boundingBox, text, options, fontProvider, fontMetrics);
        }

        if (options.IsMultiline)
        {
            return CalculateMultilineLayout(fontName, boundingBox, text, options, fontProvider, fontMetrics);
        }

        return CalculateSingleLineLayout(fontName, boundingBox, text, options, fontProvider, fontMetrics);
    }

    private static TextLayout CalculateSingleLineLayout(
        string fontName,
        Rectangle boundingBox,
        string text,
        TextFitOptions options,
        IFontMetricsProvider fontProvider,
        FontMetrics fontMetrics)
    {
        var usableWidth = Math.Max(0d, (double)boundingBox.Width - (_horizontalInset * 2));
        var usableHeight = Math.Max(0d, (double)boundingBox.Height - (_clipInset * 2));
        var autoSize = !options.RequestedFontSize.HasValue || options.RequestedFontSize.Value <= 0;

        var fontSize = autoSize
            ? Math.Max(_minFontSize, usableHeight * _autoFontSizeToUsableHeightRatio)
            : options.RequestedFontSize!.Value;

        var textWidth = fontProvider.MeasureText(text, fontName, fontSize);

        while (autoSize && textWidth > usableWidth && fontSize > _minFontSize)
        {
            fontSize -= 0.01d;
            textWidth = fontProvider.MeasureText(text, fontName, fontSize);
        }

        fontSize = Math.Max(fontSize, _minFontSize);

        var scaledXHeight = (ResolveEffectiveXHeight(fontMetrics) / 1000d) * fontSize;
        var fieldMidpointY = (double)boundingBox.LowerLeft.Y + ((double)boundingBox.Height / 2d);
        var opticalBaseline = fieldMidpointY - (_opticalBaselineAdjustment * scaledXHeight);
        var textOriginX = CalculateHorizontalOrigin(
            options.Quadding,
            (double)boundingBox.LowerLeft.X,
            usableWidth,
            textWidth);

        return new TextLayout
        {
            FontSize = fontSize,
            Segments =
            [
                new TextLayoutSegment
                {
                    Text = text,
                    Origin = new Coordinate(textOriginX, opticalBaseline)
                }
            ]
        };
    }

    private static TextLayout CalculateMultilineLayout(
        string fontName,
        Rectangle boundingBox,
        string text,
        TextFitOptions options,
        IFontMetricsProvider fontProvider,
        FontMetrics fontMetrics)
    {
        var usableWidth = Math.Max(0d, (double)boundingBox.Width - (_horizontalInset * 2));
        var usableHeight = Math.Max(0d, (double)boundingBox.Height - (_clipInset * 2));
        var autoSize = !options.RequestedFontSize.HasValue || options.RequestedFontSize.Value <= 0;

        var fontSize = autoSize
            ? Math.Max(_minFontSize, usableHeight * _autoFontSizeToUsableHeightRatio)
            : options.RequestedFontSize!.Value;

        List<string> lines;
        while (true)
        {
            lines = WrapMultilineText(text, usableWidth, fontProvider, fontName, fontSize);
            var lineHeight = CalculateLineHeight(fontSize);
            var totalHeight = CalculateMultilineContentHeight(lines.Count, lineHeight, fontMetrics, fontSize);

            if (!autoSize || (totalHeight <= usableHeight && lines.All(line => fontProvider.MeasureText(line, fontName, fontSize) <= usableWidth)))
            {
                break;
            }

            if (fontSize <= _minFontSize)
            {
                break;
            }

            fontSize = Math.Max(_minFontSize, fontSize - 0.1d);
        }

        var ascent = ScaleMetric(fontMetrics.Ascent, fontSize);
        var lineAdvance = CalculateLineHeight(fontSize);
        var firstBaseline = Math.Max(
            _clipInset + ascent,
            (double)boundingBox.UpperRight.Y - _multilineTopInset - ascent);

        var segments = new List<TextLayoutSegment>(lines.Count);
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var lineWidth = fontProvider.MeasureText(line, fontName, fontSize);
            segments.Add(new TextLayoutSegment
            {
                    Text = line,
                    Origin = new Coordinate(
                    CalculateHorizontalOrigin(
                        options.Quadding,
                        (double)boundingBox.LowerLeft.X,
                        usableWidth,
                        lineWidth),
                    firstBaseline - (index * lineAdvance))
            });
        }

        return new TextLayout
        {
            FontSize = fontSize,
            Segments = segments
        };
    }

    private static TextLayout CalculateCombLayout(
        string fontName,
        Rectangle boundingBox,
        string text,
        TextFitOptions options,
        IFontMetricsProvider fontProvider,
        FontMetrics fontMetrics)
    {
        var maxLength = Math.Max(1, options.MaxLength ?? 1);
        var visibleText = text.Length > maxLength ? text[..maxLength] : text;
        var usableWidth = Math.Max(0d, (double)boundingBox.Width - (_clipInset * 2));
        var usableHeight = Math.Max(0d, (double)boundingBox.Height - (_clipInset * 2));
        var cellWidth = usableWidth / maxLength;
        var autoSize = !options.RequestedFontSize.HasValue || options.RequestedFontSize.Value <= 0;

        var fontSize = autoSize
            ? Math.Max(_minFontSize, usableHeight * _autoFontSizeToUsableHeightRatio)
            : options.RequestedFontSize!.Value;

        while (autoSize && fontSize > _minFontSize)
        {
            var widestGlyph = visibleText.Length == 0
                ? 0d
                : visibleText.Max(ch => fontProvider.MeasureText(ch.ToString(), fontName, fontSize));

            if (widestGlyph <= Math.Max(0d, cellWidth - (_horizontalInset * 0.5d)))
            {
                break;
            }

            fontSize = Math.Max(_minFontSize, fontSize - 0.1d);
        }

        var scaledXHeight = (ResolveEffectiveXHeight(fontMetrics) / 1000d) * fontSize;
        var fieldMidpointY = (double)boundingBox.LowerLeft.Y + ((double)boundingBox.Height / 2d);
        var opticalBaseline = fieldMidpointY - (_opticalBaselineAdjustment * scaledXHeight);

        var segments = new List<TextLayoutSegment>(visibleText.Length);
        for (var index = 0; index < visibleText.Length; index++)
        {
            var glyph = visibleText[index].ToString();
            var glyphWidth = fontProvider.MeasureText(glyph, fontName, fontSize);
            var cellLeft = (double)boundingBox.LowerLeft.X + _clipInset + (index * cellWidth);
            var x = cellLeft + ((cellWidth - glyphWidth) / 2d);

            segments.Add(new TextLayoutSegment
            {
                Text = glyph,
                Origin = new Coordinate(x, opticalBaseline)
            });
        }

        return new TextLayout
        {
            FontSize = fontSize,
            Segments = segments
        };
    }

    private static List<string> WrapMultilineText(
        string text,
        double availableWidth,
        IFontMetricsProvider fontProvider,
        string fontName,
        double fontSize)
    {
        var normalizedText = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var paragraphs = normalizedText.Split('\n');
        var lines = new List<string>();

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            var currentLine = string.Empty;
            foreach (Match tokenMatch in _lineTokenRegex.Matches(paragraph))
            {
                var token = tokenMatch.Value;
                var candidateLine = currentLine + token;

                if (currentLine.Length == 0 || fontProvider.MeasureText(candidateLine, fontName, fontSize) <= availableWidth)
                {
                    currentLine = candidateLine;
                    continue;
                }

                lines.Add(currentLine.TrimEnd());
                currentLine = token.TrimStart();

                while (currentLine.Length > 0 && fontProvider.MeasureText(currentLine, fontName, fontSize) > availableWidth)
                {
                    var splitIndex = FindLargestPrefixThatFits(currentLine, availableWidth, fontProvider, fontName, fontSize);
                    lines.Add(currentLine[..splitIndex]);
                    currentLine = currentLine[splitIndex..].TrimStart();
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.TrimEnd());
            }
        }

        return lines.Count == 0 ? [string.Empty] : lines;
    }

    private static int FindLargestPrefixThatFits(
        string text,
        double availableWidth,
        IFontMetricsProvider fontProvider,
        string fontName,
        double fontSize)
    {
        for (var length = text.Length; length > 1; length--)
        {
            if (fontProvider.MeasureText(text[..length], fontName, fontSize) <= availableWidth)
            {
                return length;
            }
        }

        return 1;
    }

    private static double CalculateMultilineContentHeight(int lineCount, double lineHeight, FontMetrics fontMetrics, double fontSize)
    {
        if (lineCount <= 0)
        {
            return 0;
        }

        return ScaleMetric(fontMetrics.Ascent, fontSize)
            + ScaleMetric(fontMetrics.Descent, fontSize)
            + ((lineCount - 1) * lineHeight);
    }

    private static double CalculateLineHeight(double fontSize) => fontSize * _lineHeightMultiplier;

    private static double ScaleMetric(int metric, double fontSize)
        => Math.Abs(metric) / 1000d * fontSize;

    private static int ResolveEffectiveXHeight(FontMetrics fontMetrics)
    {
        if (fontMetrics.XHeight > 0)
        {
            return fontMetrics.XHeight;
        }

        if (fontMetrics.CapHeight > 0)
        {
            return (int)Math.Round(fontMetrics.CapHeight * _capHeightToXHeightFallbackRatio);
        }

        if (fontMetrics.Ascent > 0)
        {
            return (int)Math.Round(fontMetrics.Ascent * _ascentToXHeightFallbackRatio);
        }

        var totalHeight = fontMetrics.Ascent - fontMetrics.Descent;
        return totalHeight > 0
            ? (int)Math.Round(totalHeight * 0.5d)
            : 500;
    }

    private static double CalculateHorizontalOrigin(int quadding, double lowerLeftX, double usableWidth, double textWidth)
    {
        var remainingWidth = Math.Max(0d, usableWidth - textWidth);
        var leftInset = lowerLeftX + _horizontalInset;

        return quadding switch
        {
            1 => leftInset + (remainingWidth / 2d),
            2 => leftInset + remainingWidth,
            _ => leftInset
        };
    }
}
