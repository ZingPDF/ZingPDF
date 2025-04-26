using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.SimpleFonts;

public class TrueTypeFontDictionary : FontDictionary
{
    private TrueTypeFontDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private TrueTypeFontDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
        : base(dictionary, pdfEditor)
    {
    }

    public TrueTypeFontDictionary(IPdfEditor pdfEditor)
        : base(Subtypes.Simple.TrueType, pdfEditor)
    {
    }

    public static TrueTypeFontDictionary FromDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
    {
        return new TrueTypeFontDictionary(dictionary, pdfEditor);
    }

    public static TrueTypeFontDictionary FromDictionary(Dictionary dictionary)
    {
        return new TrueTypeFontDictionary(dictionary);
    }
}
