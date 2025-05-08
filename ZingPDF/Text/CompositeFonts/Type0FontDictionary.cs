using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.CompositeFonts;

public class Type0FontDictionary : FontDictionary
{
    private Type0FontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private Type0FontDictionary(Dictionary<string, IPdfObject> dictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
        : base(dictionary, pdfContext, objectOrigin)
    {
    }

    public Type0FontDictionary(IPdfContext pdfContext, ObjectOrigin objectOrigin)
        : base(Subtypes.Composite.Type0, pdfContext, objectOrigin)
    {
    }

    public static Type0FontDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
    {
        return new Type0FontDictionary(dictionary, pdfContext, objectOrigin);
    }

    public static Type0FontDictionary FromDictionary(Dictionary dictionary)
    {
        return new Type0FontDictionary(dictionary);
    }
}
