using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

namespace ZingPDF.Text.SimpleFonts;

public class Type3FontDictionary : FontDictionary
{
    private Type3FontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private Type3FontDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
        : base(dictionary, pdfEditor)
    {
    }

    public Type3FontDictionary(IPdfEditor pdfEditor)
        : base(Subtypes.Simple.Type3, pdfEditor)
    {
    }

    /// <summary>
    /// <para>
    /// (Required) A rectangle (see 7.9.5, "Rectangles") expressed in the glyph coordinate system, specifying the font bounding box. 
    /// This is the smallest rectangle enclosing all marks that would result if all of the glyphs of the font were placed with their 
    /// origins coincident and their descriptions executed.
    /// </para>
    /// <para>
    /// If all four elements of the rectangle are zero, a PDF processor shall make no assumptions about glyph sizes based on the font 
    /// bounding box. If any element is non-zero, the font bounding box shall be accurate. If any glyph’s marks fall outside this 
    /// bounding box, behaviour is implementation dependent and may not match the creator’s expectations.
    /// </para>
    /// </summary>
    public DictionaryProperty<Rectangle> FontBBox => Get<Rectangle>(Constants.DictionaryKeys.Font.Type3.FontBBox);

    /// <summary>
    /// <para>
    /// (Required) An array of six numbers specifying the font matrix, mapping glyph space to text space (see 9.2.4, "Glyph positioning and metrics").
    /// </para>
    /// <para>
    /// NOTE A common practice is to define glyphs in terms of a 1000-unit glyph coordinate system, in which case the font matrix is 
    /// [0.001 0 0 0.001 0 0].
    /// </para>
    /// </summary>
    public DictionaryProperty<ArrayObject> FontMatrix => Get<ArrayObject>(Constants.DictionaryKeys.Font.Type3.FontMatrix);

    /// <summary>
    /// (Required) A dictionary in which each key shall be a glyph name and the value associated with that key shall be a content stream 
    /// that constructs and paints the glyph for that character. The stream shall include as its first operator either d0 or d1, followed 
    /// by operators describing one or more graphics objects. See below for more details about Type 3 glyph descriptions.
    /// </summary>
    public DictionaryProperty<Dictionary> CharProcs => Get<Dictionary>(Constants.DictionaryKeys.Font.Type3.CharProcs);

    /// <summary>
    /// (Optional but should be used; PDF 1.2) A list of the named resources, such as fonts and images, required by the glyph descriptions 
    /// in this font (see 7.8.3, "Resource dictionaries"). If any glyph descriptions refer to named resources but this dictionary is absent, 
    /// the names shall be looked up in the resource dictionary of the page on which the font is used.
    /// </summary>
    public DictionaryProperty<Dictionary?> Resources => Get<Dictionary?>(Constants.DictionaryKeys.Font.Type3.Resources);

    public static Type3FontDictionary FromDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
    {
        return new Type3FontDictionary(dictionary, pdfEditor);
    }

    public static Type3FontDictionary FromDictionary(Dictionary dictionary)
    {
        return new Type3FontDictionary(dictionary);
    }
}
