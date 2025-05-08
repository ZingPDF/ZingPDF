using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.SimpleFonts;

public class TrueTypeFontDictionary : FontDictionary
{
    private TrueTypeFontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private TrueTypeFontDictionary(Dictionary<string, IPdfObject> dictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
        : base(dictionary, pdfContext, objectOrigin)
    {
    }

    public TrueTypeFontDictionary(IPdfContext pdfContext, ObjectOrigin objectOrigin)
        : base(Subtypes.Simple.TrueType, pdfContext, objectOrigin)
    {
    }

    public static TrueTypeFontDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
    {
        return new TrueTypeFontDictionary(dictionary, pdfContext, objectOrigin);
    }

    public static TrueTypeFontDictionary FromDictionary(Dictionary dictionary)
    {
        return new TrueTypeFontDictionary(dictionary);
    }
}
