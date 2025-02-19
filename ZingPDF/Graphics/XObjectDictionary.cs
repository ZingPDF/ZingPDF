using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Graphics
{
    public abstract class XObjectDictionary : StreamDictionary
    {
        protected static class Subtypes
        {
            public const string Form = "Form";
            public const string Image = "Image";
        }

        protected XObjectDictionary(
            Name subtype,
            Integer length,
            ShorthandArrayObject? filter,
            ShorthandArrayObject? decodeParms,
            Dictionary? f,
            ShorthandArrayObject? fFilter,
            ShorthandArrayObject? fDecodeParms,
            Integer? dL
            )
            : base(
                  Constants.DictionaryTypes.XObject,
                  length,
                  filter,
                  decodeParms,
                  f,
                  fFilter,
                  fDecodeParms,
                  dL
                  )
        {
            ArgumentNullException.ThrowIfNull(subtype);

            Set(Constants.DictionaryKeys.Subtype, subtype);
        }
        
        protected XObjectDictionary(Dictionary streamDictionary) : base(streamDictionary) { }
    }
}
