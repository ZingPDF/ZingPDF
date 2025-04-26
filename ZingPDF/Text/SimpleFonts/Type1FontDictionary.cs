using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.SimpleFonts;

public class Type1FontDictionary : FontDictionary
{
    private Type1FontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private Type1FontDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
        : base(dictionary, pdfEditor)
    {
    }

    public Type1FontDictionary(IPdfEditor pdfEditor)
        : base(Subtypes.Simple.Type1, pdfEditor)
    {
    }

    public static Type1FontDictionary FromDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
    {
        return new Type1FontDictionary(dictionary, pdfEditor);
    }

    public static Type1FontDictionary FromDictionary(Dictionary dictionary)
    {
        return new Type1FontDictionary(dictionary);
    }
}
