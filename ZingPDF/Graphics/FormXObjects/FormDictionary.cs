using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Graphics.FormXObjects;

/// <summary>
/// ISO 32000-2:2020 8.10.2 - Form dictionaries
/// </summary>
internal abstract class FormDictionary : XObjectDictionary
{
    protected FormDictionary(Dictionary dict) : base(dict) { }

    public FormDictionary(
        Number length,
        ShorthandArrayObject? filter,
        ShorthandArrayObject? decodeParms,
        Dictionary? f,
        ShorthandArrayObject? fFilter,
        ShorthandArrayObject? fDecodeParms,
        Number? dL
    ) : base(
            Subtypes.Form,
            length,
            filter,
            decodeParms,
            f,
            fFilter,
            fDecodeParms,
            dL
        )
    {
    }
}
