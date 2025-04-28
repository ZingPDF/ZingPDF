using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Text.CompositeFonts;

public class CIDSystemInfoDictionary : Dictionary
{
    public CIDSystemInfoDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
        : base(dictionary, pdfEditor)
    {
    }

    /// <summary>
    /// (Required) A string identifying the issuer of the character collection. The string shall begin with the 4 or 5 characters of 
    /// a registered developer prefix followed by a LOW LINE (5Fh) followed by any other identifying characters chosen by the issuer. 
    /// See Annex E, "Extending PDF", for how to obtain a unique developer prefix.
    /// </summary>
    public RequiredProperty<LiteralString> Registry => GetRequiredProperty<LiteralString>(Constants.DictionaryKeys.Font.CIDSystemInfo.Registry);

    /// <summary>
    /// (Required) A string that uniquely names the character collection within the specified registry.
    /// </summary>
    public RequiredProperty<LiteralString> Ordering => GetRequiredProperty<LiteralString>(Constants.DictionaryKeys.Font.CIDSystemInfo.Ordering);

    /// <summary>
    /// (Required) The supplement number of the character collection. An original character collection has a supplement number of 0. 
    /// Whenever additional CIDs are assigned in a character collection, the supplement number shall be increased. Supplements shall 
    /// not alter the ordering of existing CIDs in the character collection. This value shall not be used in determining compatibility 
    /// between character collections.
    /// </summary>
    public RequiredProperty<Number> Supplement => GetRequiredProperty<Number>(Constants.DictionaryKeys.Font.CIDSystemInfo.Supplement);

    public static CIDSystemInfoDictionary FromDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
    {
        return new CIDSystemInfoDictionary(dictionary, pdfEditor);
    }
}