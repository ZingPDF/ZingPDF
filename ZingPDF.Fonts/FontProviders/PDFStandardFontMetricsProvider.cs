using System.Reflection;
using ZingPDF.Fonts.AFM;

namespace ZingPDF.Fonts.FontProviders;
/// <summary>
/// Provider for the 14 standard PDF fonts. Metrics are taken from Adobe Font Metrics (AFM) files.
/// </summary>
public class PDFStandardFontMetricsProvider : IFontMetricsProvider
{
    private readonly Lazy<Dictionary<string, FontMetrics>> _fontMetrics;

    public PDFStandardFontMetricsProvider()
    {
        _fontMetrics = new(LoadStandardFonts);
    }

    /// <summary>
    /// Calculate text width using a standard font
    /// </summary>
    public double MeasureText(string text, string fontName, double fontSize)
    {
        var metrics = GetFontMetrics(fontName);

        return metrics.CalculateStringWidth(text, fontSize);
    }

    /// <summary>
    /// Check if a font is one of the standard PDF fonts
    /// </summary>
    public bool IsSupported(string fontName)
    {
        string normalizedName = NormalizeFontName(fontName);

        return _fontMetrics.Value.ContainsKey(normalizedName);
    }

    /// <summary>
    /// Get metrics for a standard font
    /// </summary>
    public FontMetrics GetFontMetrics(string fontName)
    {
        // Handle font aliases
        string normalizedName = NormalizeFontName(fontName);

        if (_fontMetrics.Value.TryGetValue(normalizedName, out var metrics))
        {
            return metrics;
        }

        throw new FontNotFoundException($"Font '{fontName}' is not a standard PDF font");
    }

    /// <summary>
    /// Load the 14 standard PDF fonts from embedded resources
    /// </summary>
    private Dictionary<string, FontMetrics> LoadStandardFonts()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourcePrefix = "ZingPDF.Fonts.AFM.Adobe_Core35_AFMs_314.";

        var fontMetrics = new Dictionary<string, FontMetrics>();

        foreach (string fontName in StandardPdfFonts.All)
        {
            string resourceName = $"{resourcePrefix}{fontName}.afm";

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                Console.WriteLine($"Warning: AFM resource not found for {fontName}");
                continue;
            }

            try
            {
                var metrics = AFMParser.Parse(stream);
                fontMetrics[fontName] = metrics;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading font {fontName}: {ex.Message}");
            }
        }

        return fontMetrics;
    }

    /// <summary>
    /// Normalize common font name variations
    /// </summary>
    private static string NormalizeFontName(string fontName)
    {
        // Handle common font name variations
        return fontName.ToLowerInvariant() switch
        {
            "times" or "timesnewroman" or "times new roman" => "Times-Roman",
            "timesbd" or "times-bold" or "times bold" => "Times-Bold",
            "timesi" or "times-italic" or "times italic" => "Times-Italic",
            "timesbi" or "times-bolditalic" or "times bold italic" => "Times-BoldItalic",
            "helvetica-regular" or "helvetica regular" => "Helvetica",
            // Add more aliases as needed
            _ => fontName,
        };
    }

    /// <summary>
    /// Backwards-compatible access to the canonical standard PDF font names.
    /// </summary>
    public static class FontNames
    {
        public const string Helvetica = StandardPdfFonts.Helvetica;
        public const string HelveticaBold = StandardPdfFonts.HelveticaBold;
        public const string HelveticaOblique = StandardPdfFonts.HelveticaOblique;
        public const string HelveticaBoldOblique = StandardPdfFonts.HelveticaBoldOblique;
        public const string TimesRoman = StandardPdfFonts.TimesRoman;
        public const string TimesBold = StandardPdfFonts.TimesBold;
        public const string TimesItalic = StandardPdfFonts.TimesItalic;
        public const string TimesBoldItalic = StandardPdfFonts.TimesBoldItalic;
        public const string Courier = StandardPdfFonts.Courier;
        public const string CourierBold = StandardPdfFonts.CourierBold;
        public const string CourierOblique = StandardPdfFonts.CourierOblique;
        public const string CourierBoldOblique = StandardPdfFonts.CourierBoldOblique;
        public const string Symbol = StandardPdfFonts.Symbol;
        public const string ZapfDingbats = StandardPdfFonts.ZapfDingbats;

        public static IEnumerable<string> All => StandardPdfFonts.All;
    }
}
