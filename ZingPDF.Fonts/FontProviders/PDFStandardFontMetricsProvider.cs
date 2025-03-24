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

        foreach (string fontName in FontNames.All)
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

    public static class FontNames
    {
        public const string Helvetica = "Helvetica";
        public const string HelveticaBold = "Helvetica-Bold";
        public const string HelveticaOblique = "Helvetica-Oblique";
        public const string HelveticaBoldOblique = "Helvetica-BoldOblique";
        public const string TimesRoman = "Times-Roman";
        public const string TimesBold = "Times-Bold";
        public const string TimesItalic = "Times-Italic";
        public const string TimesBoldItalic = "Times-BoldItalic";
        public const string Courier = "Courier";
        public const string CourierBold = "Courier-Bold";
        public const string CourierOblique = "Courier-Oblique";
        public const string CourierBoldOblique = "Courier-BoldOblique";
        public const string Symbol = "Symbol";
        public const string ZapfDingbats = "ZapfDingbats";

        public static IEnumerable<string> All => [
            Helvetica, HelveticaBold, HelveticaOblique, HelveticaBoldOblique,
            TimesRoman, TimesBold, TimesItalic, TimesBoldItalic,
            Courier, CourierBold, CourierOblique, CourierBoldOblique,
            Symbol, ZapfDingbats
        ];
    }
}
