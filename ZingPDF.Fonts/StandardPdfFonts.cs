namespace ZingPDF.Fonts;

/// <summary>
/// Canonical names for the standard PDF font set.
/// </summary>
public static class StandardPdfFonts
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

    public static IReadOnlyList<string> All { get; } =
    [
        Helvetica, HelveticaBold, HelveticaOblique, HelveticaBoldOblique,
        TimesRoman, TimesBold, TimesItalic, TimesBoldItalic,
        Courier, CourierBold, CourierOblique, CourierBoldOblique,
        Symbol, ZapfDingbats
    ];
}
