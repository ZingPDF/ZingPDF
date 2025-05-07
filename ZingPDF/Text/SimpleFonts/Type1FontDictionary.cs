using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.SimpleFonts;

public class Type1FontDictionary : FontDictionary
{
    private Type1FontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private Type1FontDictionary(Dictionary<string, IPdfObject> dictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
        : base(dictionary, pdfContext, objectOrigin)
    {
    }

    public Type1FontDictionary(IPdfContext pdfContext, ObjectOrigin objectOrigin)
        : base(Subtypes.Simple.Type1, pdfContext, objectOrigin)
    {
    }

    public static Type1FontDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
    {
        return new Type1FontDictionary(dictionary, pdfContext, objectOrigin);
    }

    public static Type1FontDictionary FromDictionary(Dictionary dictionary)
    {
        return new Type1FontDictionary(dictionary);
    }
}
