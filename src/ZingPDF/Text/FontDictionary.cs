using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Text.Encoding;

namespace ZingPDF.Text;

public abstract class FontDictionary : Dictionary
{
    public static class Subtypes
    {
        public static class Simple
        {
            public const string Type1 = "Type1";
            public const string TrueType = "TrueType";
            public const string Type3 = "Type3";
        }

        public static class Composite
        {
            public const string Type0 = "Type0";
        }

        public static class CID
        {
            public const string CIDFontType0 = "CIDFontType0";
            public const string CIDFontType2 = "CIDFontType2";
        }
    }

    protected FontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    protected FontDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
        : base(dictionary, pdf, context)
    {
    }

    protected FontDictionary(Name subType, IPdf pdf, ObjectContext context)
        : base(Constants.DictionaryTypes.Font, pdf, context)
    {
        ArgumentNullException.ThrowIfNull(subType);

        Set(Constants.DictionaryKeys.Subtype, subType);
    }

    /// <summary>
    /// (Required for most simple fonts) The PostScript name of the font.
    /// For Type 1 and TrueType fonts, this is usually the value of the <c>FontName</c> entry in the font program.
    /// It may be used to locate or substitute the font in the PDF processor’s environment.
    /// For Type 3 fonts, it is an arbitrary name identifying the font.
    /// For composite fonts (Type 0), <c>BaseFont</c> is the name of the CIDFont that serves as the descendant font.
    /// CIDFonts themselves may omit this entry.
    /// </summary>
    public OptionalProperty<Name> BaseFont => GetOptionalProperty<Name>(Constants.DictionaryKeys.Font.BaseFont);

    /// <summary>
    /// (Optional; required for some simple fonts) Defines the mapping from character codes to glyph names or Unicode values.
    /// For Type 1 and TrueType fonts, this may be a predefined encoding name (such as <c>/WinAnsiEncoding</c>) or a dictionary.
    /// For Type 3 fonts, this entry is required and determines the glyph procedure to invoke for each character code.
    /// Composite fonts (Type 0) do not use this entry; they use CMaps for character code mapping.
    /// </summary>
    public OptionalMultiProperty<Name, EncodingDictionary> Encoding => GetOptionalMultiProperty<Name, EncodingDictionary>(Constants.DictionaryKeys.Font.Encoding);

    /// <summary>
    /// (Required for Type 1, TrueType, and Type 3 fonts) The first character code for which widths are specified in the <c>/Widths</c> array.
    /// This is an integer between 0 and 255, inclusive. The character codes must be contiguous and start with this value.
    /// Not used for composite fonts (Type 0) or CIDFonts; those use CID-based mappings.
    /// </summary>
    public OptionalProperty<Number> FirstChar => GetOptionalProperty<Number>(Constants.DictionaryKeys.Font.FirstChar);

    /// <summary>
    /// (Required for Type 1, TrueType, and Type 3 fonts) The last character code for which widths are specified in the <c>/Widths</c> array.
    /// This is an integer between 0 and 255, inclusive, and must be greater than or equal to <c>/FirstChar</c>.
    /// Not used for composite fonts (Type 0) or CIDFonts.
    /// </summary>
    public OptionalProperty<Number> LastChar => GetOptionalProperty<Number>(Constants.DictionaryKeys.Font.LastChar);

    /// <summary>
    /// (Required for Type 1, TrueType, and Type 3 fonts) An array of widths, in glyph space units, for character codes ranging from
    /// <c>/FirstChar</c> to <c>/LastChar</c> inclusive. Each width applies to the character code at that position in the range.
    /// Not used for composite fonts (Type 0) or CIDFonts; they use <c>/W</c> and <c>/DW</c> arrays instead.
    /// </summary>
    public OptionalProperty<ArrayObject> Widths => GetOptionalProperty<ArrayObject>(Constants.DictionaryKeys.Font.Widths);

    /// <summary>
    /// (Required for fonts that are embedded or need detailed metrics)
    /// A dictionary containing font-wide metrics and characteristics, including bounding box, ascent, descent, stem width, etc.
    /// For most font types (Type 1, TrueType, Type 3), this entry is optional but recommended.
    /// If the font program is embedded (via <c>/FontFile</c>, <c>/FontFile2</c>, or <c>/FontFile3</c>), the descriptor is required.
    /// For CIDFonts used with composite fonts (Type 0), this entry is required and contains essential metrics for layout and rendering.
    /// </summary>
    public OptionalProperty<FontDescriptorDictionary> FontDescriptor => GetOptionalProperty<FontDescriptorDictionary>(Constants.DictionaryKeys.Font.FontDescriptor);

    /// <summary>
    /// (Optional) A stream containing a CMap that maps character codes to Unicode values.
    /// This entry enables accurate text extraction and search operations.
    /// Applicable to all font types, including simple fonts and composite fonts.
    /// Strongly recommended when using custom encodings or CIDFonts, as it provides a Unicode mapping independent of glyph names or positions.
    /// </summary>
    public OptionalProperty<StreamObject<StreamDictionary>> ToUnicode => GetOptionalProperty<StreamObject<StreamDictionary>>(Constants.DictionaryKeys.Font.ToUnicode);
}
