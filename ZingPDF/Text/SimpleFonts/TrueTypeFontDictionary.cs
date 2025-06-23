using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.SimpleFonts;

public class TrueTypeFontDictionary : FontDictionary
{
    private TrueTypeFontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private TrueTypeFontDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectOrigin objectOrigin)
        : base(dictionary, pdf, objectOrigin)
    {
    }

    public TrueTypeFontDictionary(IPdf pdf, ObjectOrigin objectOrigin)
        : base(Subtypes.Simple.TrueType, pdf, objectOrigin)
    {
    }

    public static TrueTypeFontDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectOrigin objectOrigin)
    {
        return new TrueTypeFontDictionary(dictionary, pdf, objectOrigin);
    }

    public static TrueTypeFontDictionary FromDictionary(Dictionary dictionary)
    {
        return new TrueTypeFontDictionary(dictionary);
    }
}
