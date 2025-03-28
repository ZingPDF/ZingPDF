using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Graphics.FormXObjects;

/// <summary>
/// ISO 32000-2:2020 8.10.2 - Form dictionaries
/// </summary>
internal abstract class FormDictionary : XObjectDictionary
{
    protected FormDictionary(Dictionary dict)
        : base(dict) { }

    protected FormDictionary(Dictionary<Name, IPdfObject> dict, IPdfEditor pdfEditor)
        : base(dict, pdfEditor) { }

    public FormDictionary(
        Number length,
        ShorthandArrayObject? filter,
        ShorthandArrayObject? decodeParms,
        Dictionary? f,
        ShorthandArrayObject? fFilter,
        ShorthandArrayObject? fDecodeParms,
        Number? dL,
        IPdfEditor pdfEditor
    ) : base(
            Subtypes.Form,
            length,
            filter,
            decodeParms,
            f,
            fFilter,
            fDecodeParms,
            dL,
            pdfEditor
        )
    {
    }
}
