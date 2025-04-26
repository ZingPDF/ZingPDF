using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.CompositeFonts;

public class Type0FontDictionary : FontDictionary
{
    private Type0FontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private Type0FontDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
        : base(dictionary, pdfEditor)
    {
    }

    public Type0FontDictionary(IPdfEditor pdfEditor)
        : base(Subtypes.Composite.Type0, pdfEditor)
    {
    }

    public static Type0FontDictionary FromDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
    {
        return new Type0FontDictionary(dictionary, pdfEditor);
    }

    public static Type0FontDictionary FromDictionary(Dictionary dictionary)
    {
        return new Type0FontDictionary(dictionary);
    }
}
