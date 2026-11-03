using System.Text;
using ZingPDF.Graphics;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Text;

/// <summary>
/// A font registered with a PDF document and ready to use from page APIs.
/// </summary>
public sealed class PdfFont
{
    internal PdfFont(
        Name resourceName,
        IndirectObjectReference fontReference,
        string baseFontName,
        FontTextEncoding textEncoding,
        bool isEmbedded, UnicodeTrueTypeFont? unicode = null, ZingPDF.Fonts.FontMetrics? metrics = null, IPdf? owner = null)
    {
        ResourceName = resourceName ?? throw new ArgumentNullException(nameof(resourceName));
        FontReference = fontReference ?? throw new ArgumentNullException(nameof(fontReference));
        BaseFontName = baseFontName ?? throw new ArgumentNullException(nameof(baseFontName));
        TextEncoding = textEncoding;
        IsEmbedded = isEmbedded;
        _unicode = unicode;
        _metrics = metrics;
        Owner = owner;
    }

    /// <summary>
    /// The resource name used to reference the font inside page content streams.
    /// </summary>
    public Name ResourceName { get; }

    /// <summary>
    /// The PDF base font name.
    /// </summary>
    public string BaseFontName { get; }

    /// <summary>
    /// Gets whether the font is embedded in the document.
    /// </summary>
    public bool IsEmbedded { get; }

    internal IndirectObjectReference FontReference { get; }
    internal FontTextEncoding TextEncoding { get; }

    internal bool IsUnicode => _unicode is not null;
    internal IPdf? Owner { get; }
    private readonly UnicodeTrueTypeFont? _unicode;
    private readonly ZingPDF.Fonts.FontMetrics? _metrics;
    private static readonly System.Text.Encoding WinAnsi = System.Text.Encoding.GetEncoding(1252,
        EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    internal ZingPDF.Fonts.FontMetrics? Metrics => _unicode?.Face.Metrics ?? _metrics;

    /// <summary>Returns distinct characters the font cannot encode. CR/LF line separators are ignored.</summary>
    public string GetMissingCharacters(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (_unicode is not null) return _unicode.GetMissingCharacters(text);
        var missing = new StringBuilder();
        foreach (var rune in text.EnumerateRunes().Distinct())
        {
            if (rune.Value is 10 or 13) continue;
            try { WinAnsi.GetByteCount(rune.ToString()); }
            catch (EncoderFallbackException) { missing.Append(rune); }
        }
        return missing.ToString();
    }

    /// <summary>
    /// Measures one line's baseline advance in text units at the supplied positive font size.
    /// Does not apply kerning, shaping, character spacing, horizontal scaling or page transforms.
    /// Missing characters and line breaks are rejected before measurement.
    /// </summary>
    public double MeasureText(string text, double fontSize)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!double.IsFinite(fontSize) || fontSize <= 0) throw new ArgumentOutOfRangeException(nameof(fontSize));
        if (text.Contains('\n') || text.Contains('\r')) throw new ArgumentException("Measure one line at a time.", nameof(text));
        var missing = GetMissingCharacters(text);
        if (missing.Length != 0) throw new ArgumentException($"The font '{BaseFontName}' cannot encode: {missing}", nameof(text));
        if (_unicode is not null) return _unicode.Measure(text) / 1000 * fontSize;
        if (_metrics is not null) return text.Sum(c => _metrics.Widths.GetValueOrDefault(c, 500)) / 1000d * fontSize;
        var metrics = new ZingPDF.Fonts.FontProviders.PDFStandardFontMetricsProvider();
        if (metrics.IsSupported(BaseFontName))
        {
            var widths = metrics.GetFontMetrics(BaseFontName).Widths;
            return text.Sum(c => widths.GetValueOrDefault(c, 500)) / 1000d * fontSize;
        }
        throw new NotSupportedException("This font does not expose baseline advance metrics.");
    }

    internal Syntax.Objects.Strings.PdfString Encode(string text)
        => Syntax.Objects.Strings.PdfString.FromBytes(_unicode?.Encode(text) ?? WinAnsi.GetBytes(text),
            _unicode is null ? Syntax.Objects.Strings.PdfStringSyntax.Literal : Syntax.Objects.Strings.PdfStringSyntax.Hex,
            Syntax.ObjectContext.UserCreated);

    /// <summary>
    /// Creates font options for use with higher-level text APIs.
    /// </summary>
    public FontOptions CreateOptions(Number size, RGBColour colour)
    {
        return new FontOptions
        {
            ResourceName = ResourceName,
            Size = size,
            Colour = colour,
            TextEncoding = TextEncoding,
            RegisteredFont = this
        };
    }
}
