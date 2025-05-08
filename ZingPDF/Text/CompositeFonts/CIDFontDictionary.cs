using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

namespace ZingPDF.Text.CompositeFonts;

public class CIDFontDictionary : FontDictionary
{
    public CIDFontDictionary(Dictionary<string, IPdfObject> dictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
        : base(dictionary, pdfContext, objectOrigin)
    {
    }

    /// <summary>
    /// (Required) A dictionary containing entries that define the character collection of the CIDFont. 
    /// See "Table 114 — Entries in a CIDSystemInfo dictionary".
    /// </summary>
    public RequiredProperty<CIDSystemInfoDictionary> CIDSystemInfo => GetRequiredProperty<CIDSystemInfoDictionary>(Constants.DictionaryKeys.Font.CID.CIDSystemInfo);

    /// <summary>
    /// (Optional) The default width for glyphs in the CIDFont (see 9.7.4.3, "Glyph metrics in CIDFonts"). Default value: 1000.
    /// </summary>
    public OptionalProperty<Number> DW => GetOptionalProperty<Number>(Constants.DictionaryKeys.Font.CID.DW);

    public static CIDFontDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
    {
        return new CIDFontDictionary(dictionary, pdfContext, objectOrigin);
    }
}
