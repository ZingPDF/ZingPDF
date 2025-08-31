using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.SimpleFonts;

public class TrueTypeFontDictionary : FontDictionary
{
    private TrueTypeFontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private TrueTypeFontDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
        : base(dictionary, pdf, context)
    {
    }

    public TrueTypeFontDictionary(IPdf pdf, ObjectContext context)
        : base(Subtypes.Simple.TrueType, pdf, context)
    {
    }

    public static TrueTypeFontDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
    {
        return new TrueTypeFontDictionary(dictionary, pdf, context);
    }

    public static TrueTypeFontDictionary FromDictionary(Dictionary dictionary)
    {
        return new TrueTypeFontDictionary(dictionary);
    }
}
