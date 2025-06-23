using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.CompositeFonts;

public class Type0FontDictionary : FontDictionary
{
    private Type0FontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private Type0FontDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectOrigin objectOrigin)
        : base(dictionary, pdf, objectOrigin)
    {
    }

    public Type0FontDictionary(IPdf pdf, ObjectOrigin objectOrigin)
        : base(Subtypes.Composite.Type0, pdf, objectOrigin)
    {
    }

    public static Type0FontDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectOrigin objectOrigin)
    {
        return new Type0FontDictionary(dictionary, pdf, objectOrigin);
    }

    public static Type0FontDictionary FromDictionary(Dictionary dictionary)
    {
        return new Type0FontDictionary(dictionary);
    }
}
