using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

namespace ZingPDF.Text.Encoding;

public class EncodingDictionary : Dictionary
{
    public EncodingDictionary(IEnumerable<KeyValuePair<string, IPdfObject>> dictionary, IPdf pdf, ObjectContext context)
        : base(dictionary, pdf, context)
    {
    }

    /// <summary>
    /// <para>
    /// (Optional) The base encoding — that is, the encoding from which the Differences entry (if present) describes 
    /// differences — shall be the name of one of the predefined encodings MacRomanEncoding, MacExpertEncoding, or 
    /// WinAnsiEncoding (see Annex D, "Character sets and encodings").
    /// </para>
    /// <para>
    /// If this entry is absent, the Differences entry shall describe differences from a default base encoding. For a font program that is embedded in the PDF file, the default base encoding shall be the font program’s built-in encoding, as described in 9.6.5, "Character encoding" and further elaborated in the subclauses on specific font types. Otherwise, for a nonsymbolic font, it shall be StandardEncoding, and for a symbolic font, it shall be the font’s built-in encoding.
    /// </para>
    /// </summary>
    public OptionalProperty<Name> BaseEncoding => GetOptionalProperty<Name>(Constants.DictionaryKeys.Encoding.BaseEncoding);

    /// <summary>
    /// (Optional; should not be used with TrueType fonts) An array describing the differences from the encoding specified 
    /// by BaseEncoding or, if BaseEncoding is absent from a default base encoding. The Differences array is described in 
    /// subsequent subclauses.
    /// </summary>
    public OptionalProperty<ArrayObject> Differences => GetOptionalProperty<ArrayObject>(Constants.DictionaryKeys.Encoding.Differences);

    public static EncodingDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
    {
        return new(dictionary, pdf, context);
    }
}
