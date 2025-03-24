using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Text;

internal class FontDescriptorDictionary : Dictionary
{
    public FontDescriptorDictionary() : base(Constants.DictionaryTypes.FontDescriptor)
    {
    }

    private FontDescriptorDictionary(Dictionary dictionary) : base(dictionary)
    {
    }

    /// <summary>
    /// (Required) The PostScript name of the font. For Type 3 fonts that include a Name entry in the Type 3 
    /// font dictionary, this name shall match the value of that key. For all fonts other than Type 3 this 
    /// name shall be the same as the value of BaseFont in the font or CIDFont dictionary that refers to 
    /// this font descriptor.
    /// </summary>
    public AsyncProperty<Name> FontName => Get<Name>(Constants.DictionaryKeys.FontDescriptor.FontName)!;

    /// <summary>
    /// <para>(Optional; PDF 1.5) A byte string specifying the preferred font family name.</para>
    /// <para>EXAMPLE 1 For the font Times Bold Italic, the FontFamily is Times.</para>
    /// </summary>
    public AsyncProperty<Name>? FontFamily => Get<Name>(Constants.DictionaryKeys.FontDescriptor.FontFamily);

    /// <summary>
    /// <para>
    /// (Optional; PDF 1.5) The font stretch value. It shall be one of these names (ordered from narrowest 
    /// to widest): UltraCondensed, ExtraCondensed, Condensed, SemiCondensed, Normal, SemiExpanded, Expanded, 
    /// ExtraExpanded or UltraExpanded.
    /// </para>
    /// <para>The specific interpretation of these values varies from font to font.</para>
    /// <para>EXAMPLE 2 Condensed in one font might appear most similar to Normal in another.</para>
    /// </summary>
    public AsyncProperty<Name>? FontStretch => Get<Name>(Constants.DictionaryKeys.FontDescriptor.FontStretch);

    /// <summary>
    /// <para>
    /// (Optional; PDF 1.5) The weight (thickness) component of the fully-qualified font name or font specifier. 
    /// If present, the value shall be one of 100, 200, 300, 400, 500, 600, 700, 800, or 900, where each number 
    /// indicates a weight that is at least as dark as its predecessor. A value of 400 shall indicate a normal 
    /// weight; 700 shall indicate bold.
    /// </para>
    /// <para>The specific interpretation of these values varies from font to font.</para>
    /// <para>EXAMPLE 3 300 in one font might appear most similar to 500 in another.</para>
    /// </summary>
    public AsyncProperty<Number>? FontWeight => Get<Number>(Constants.DictionaryKeys.FontDescriptor.FontWeight);

    /// <summary>
    /// (Required) A collection of flags defining various characteristics of the font (see 9.8.2, "Font descriptor flags").
    /// </summary>
    public AsyncProperty<Number> Flags => Get<Number>(Constants.DictionaryKeys.FontDescriptor.Flags)!;

    /// <summary>
    /// (Required except for Type 3 fonts) A rectangle (see 7.9.5, "Rectangles"), expressed in the glyph coordinate 
    /// system, that shall specify the font bounding box. This should be the smallest rectangle enclosing the shape 
    /// that would result if all of the glyphs of the font were placed with their origins coincident and then filled.
    /// </summary>
    public AsyncProperty<Rectangle>? FontBBox => Get<Rectangle>(Constants.DictionaryKeys.FontDescriptor.FontBBox);

    /// <summary>
    /// <para>
    /// (Required) The angle, expressed in degrees counterclockwise from the vertical, of the dominant vertical 
    /// strokes of the font.
    /// </para>
    /// <para>EXAMPLE 4 The 9-o’clock position is 90 degrees, and the 3-o’clock position is –90 degrees.</para>
    /// <para>The value shall be negative for fonts that slope to the right, as almost all italic fonts do</para>
    /// </summary>
    public AsyncProperty<Number> ItalicAngle => Get<Number>(Constants.DictionaryKeys.FontDescriptor.ItalicAngle)!;

    /// <summary>
    /// (Required, except for Type 3 fonts) The maximum height above the baseline reached by glyphs in this font. 
    /// The height of glyphs for accented characters shall be excluded.
    /// </summary>
    public AsyncProperty<Number>? Ascent => Get<Number>(Constants.DictionaryKeys.FontDescriptor.Ascent);

    /// <summary>
    /// (Required, except for Type 3 fonts) The maximum depth below the baseline reached by glyphs in this font. 
    /// The value shall be a negative number.
    /// </summary>
    public AsyncProperty<Number>? Descent => Get<Number>(Constants.DictionaryKeys.FontDescriptor.Descent);

    /// <summary>
    /// <para>(Optional) The spacing between baselines of consecutive lines of text.</para>
    /// <para>Default value: 0.</para>
    /// </summary>
    public AsyncProperty<Number>? Leading => Get<Number>(Constants.DictionaryKeys.FontDescriptor.Leading);

    /// <summary>
    /// (Required for fonts that have Latin characters, except for Type 3 fonts) The vertical coordinate of the top 
    /// of flat capital letters, measured from the baseline.
    /// </summary>
    public AsyncProperty<Number>? CapHeight => Get<Number>(Constants.DictionaryKeys.FontDescriptor.CapHeight);

    /// <summary>
    /// (Optional) The font’s x height: the vertical coordinate of the top of flat nonascending lowercase letters 
    /// (like the letter x), measured from the baseline, in fonts that have Latin characters. Default value: 0.
    /// </summary>
    public AsyncProperty<Number>? XHeight => Get<Number>(Constants.DictionaryKeys.FontDescriptor.XHeight);

    /// <summary>
    /// (Required except for Type 3 fonts) The thickness measured horizontally, of the dominant vertical stems of 
    /// glyphs in the font. Values shall be positive. A value of 0 indicates an unknown stem thickness.
    /// </summary>
    public AsyncProperty<Number>? StemV => Get<Number>(Constants.DictionaryKeys.FontDescriptor.StemV);

    /// <summary>
    /// (Optional) The thickness measured vertically of the dominant horizontal stems of glyphs in the font. Values 
    /// shall be positive. A value of 0 indicates an unknown stem thickness. Default value: 0.
    /// </summary>
    public AsyncProperty<Number>? StemH => Get<Number>(Constants.DictionaryKeys.FontDescriptor.StemH);

    /// <summary>
    /// (Optional) The average width of glyphs in the font. Default value: 0.
    /// </summary>
    public AsyncProperty<Number>? AvgWidth => Get<Number>(Constants.DictionaryKeys.FontDescriptor.AvgWidth);

    /// <summary>
    /// (Optional) The maximum width of glyphs in the font. Default value: 0.
    /// </summary>
    public AsyncProperty<Number>? MaxWidth => Get<Number>(Constants.DictionaryKeys.FontDescriptor.MaxWidth);

    /// <summary>
    /// (Optional) The width to use for character codes whose widths are not specified in a font dictionary’s Widths 
    /// array. This shall have a predictable effect only if all such codes map to glyphs whose actual widths are the 
    /// same as the value of the MissingWidth entry. Default value: 0.
    /// </summary>
    public AsyncProperty<Number>? MissingWidth => Get<Number>(Constants.DictionaryKeys.FontDescriptor.MissingWidth);

    /// <summary>
    /// (Optional) A stream containing a Type 1 font program (see 9.9, "Embedded font programs").
    /// </summary>
    public AsyncProperty<StreamObject<IStreamDictionary>>? FontFile => Get<StreamObject<IStreamDictionary>>(Constants.DictionaryKeys.FontDescriptor.FontFile);

    /// <summary>
    /// (Optional; PDF 1.1) A stream containing a TrueType font program (see 9.9, "Embedded font programs").
    /// </summary>
    public AsyncProperty<StreamObject<IStreamDictionary>>? FontFile2 => Get<StreamObject<IStreamDictionary>>(Constants.DictionaryKeys.FontDescriptor.FontFile2);

    /// <summary>
    /// (Optional; PDF 1.2) A stream containing a font program whose format is specified by the Subtype entry in the 
    /// stream dictionary (see "Table 124 — Embedded font organisation for various font types").
    /// </summary>
    public AsyncProperty<StreamObject<IStreamDictionary>>? FontFile3 => Get<StreamObject<IStreamDictionary>>(Constants.DictionaryKeys.FontDescriptor.FontFile3);

    /// <summary>
    /// (Optional; meaningful only in Type 1 fonts; PDF 1.1; deprecated in PDF 2.0) A string listing the character names 
    /// defined in a font subset. The names in this string shall be in PDF syntax — that is, each name preceded by a slash (/). 
    /// The names may appear in any order. The name .notdef shall be omitted; it shall exist in the font subset. If this 
    /// entry is absent, the only indication of a font subset shall be the subset tag in the FontName entry (see 9.9.2, "Font subsets").
    /// </summary>
    public AsyncProperty<Either<LiteralString, HexadecimalString>>? CharSet
        => Get<Either<LiteralString, HexadecimalString>>(Constants.DictionaryKeys.FontDescriptor.CharSet);

    internal static FontDescriptorDictionary FromDictionary(Dictionary dictionary)
    {
        return new FontDescriptorDictionary(dictionary);
    }
}
