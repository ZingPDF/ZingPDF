using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.SimpleFonts;

public class Type1FontDictionary : FontDictionary
{
    private Type1FontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private Type1FontDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectOrigin objectOrigin)
        : base(dictionary, pdf, objectOrigin)
    {
    }

    public Type1FontDictionary(IPdf pdf, ObjectOrigin objectOrigin)
        : base(Subtypes.Simple.Type1, pdf, objectOrigin)
    {
    }

    public static Type1FontDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectOrigin objectOrigin)
    {
        return new Type1FontDictionary(dictionary, pdf, objectOrigin);
    }

    public static Type1FontDictionary FromDictionary(Dictionary dictionary)
    {
        return new Type1FontDictionary(dictionary);
    }
}
