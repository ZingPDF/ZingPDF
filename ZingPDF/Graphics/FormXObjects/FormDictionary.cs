using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Graphics.FormXObjects
{
    /// <summary>
    /// ISO 32000-2:2020 8.10.2 - Form dictionaries
    /// </summary>
    internal abstract class FormDictionary(
        Integer length,
        ShorthandArrayObject? filter,
        ShorthandArrayObject? decodeParms,
        Dictionary? f,
        ShorthandArrayObject? fFilter,
        ShorthandArrayObject? fDecodeParms,
        Integer? dL
        ) : XObjectDictionary(
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
