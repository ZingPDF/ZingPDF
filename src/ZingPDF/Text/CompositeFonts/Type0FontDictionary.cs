using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.CompositeFonts;

public class Type0FontDictionary : FontDictionary
{
    private Type0FontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private Type0FontDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
        : base(dictionary, pdf, context)
    {
    }

    public Type0FontDictionary(IPdf pdf, ObjectContext context)
        : base(Subtypes.Composite.Type0, pdf, context)
    {
    }

    public static Type0FontDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
    {
        return new Type0FontDictionary(dictionary, pdf, context);
    }

    public static Type0FontDictionary FromDictionary(Dictionary dictionary)
    {
        return new Type0FontDictionary(dictionary);
    }
}
