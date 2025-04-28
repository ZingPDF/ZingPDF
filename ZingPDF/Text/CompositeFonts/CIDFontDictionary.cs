using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

namespace ZingPDF.Text.CompositeFonts;

public class CIDFontDictionary : Dictionary
{
    public CIDFontDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
        : base(dictionary, pdfEditor)
    {
    }

    /// <summary>
    /// (Required) The PostScript name of the CIDFont. For Type 0 CIDFonts, this shall be the value of the CIDFontName 
    /// entry in the CIDFont program. For Type 2 CIDFonts, it shall be derived the same way as for a simple TrueType 
    /// font; see 9.6.3, "TrueType fonts". In either case, the name may have a subset prefix if appropriate; see 9.9.2, 
    /// "Font subsets".
    /// </summary>
    public RequiredProperty<Name> BaseFont => GetRequiredProperty<Name>(Constants.DictionaryKeys.Font.BaseFont);

    /// <summary>
    /// (Required) A dictionary containing entries that define the character collection of the CIDFont. 
    /// See "Table 114 — Entries in a CIDSystemInfo dictionary".
    /// </summary>
    public RequiredProperty<CIDSystemInfoDictionary> CIDSystemInfo => GetRequiredProperty<CIDSystemInfoDictionary>(Constants.DictionaryKeys.Font.CID.CIDSystemInfo);

    /// <summary>
    /// (Required; shall be an indirect reference) A font descriptor describing the CIDFont’s default metrics other than its glyph 
    /// widths (see 9.8, "Font descriptors").
    /// </summary>
    public RequiredProperty<FontDescriptorDictionary> FontDescriptor => GetRequiredProperty<FontDescriptorDictionary>(Constants.DictionaryKeys.Font.FontDescriptor);

    /// <summary>
    /// (Optional) The default width for glyphs in the CIDFont (see 9.7.4.3, "Glyph metrics in CIDFonts"). Default value: 1000.
    /// </summary>
    public OptionalProperty<Number> DW => GetOptionalProperty<Number>(Constants.DictionaryKeys.Font.CID.DW);



    public static CIDFontDictionary FromDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
    {
        return new CIDFontDictionary(dictionary, pdfEditor);
    }
}
