using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.SimpleFonts;

public class Type1FontDictionary : FontDictionary
{
    private Type1FontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private Type1FontDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
        : base(dictionary, pdf, context)
    {
    }

    public Type1FontDictionary(IPdf pdf, ObjectContext context)
        : base(Subtypes.Simple.Type1, pdf, context)
    {
    }

    public static Type1FontDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
    {
        return new Type1FontDictionary(dictionary, pdf, context);
    }

    public static Type1FontDictionary FromDictionary(Dictionary dictionary)
    {
        return new Type1FontDictionary(dictionary);
    }
}
