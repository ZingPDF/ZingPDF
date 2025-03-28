using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Graphics
{
    public abstract class XObjectDictionary : StreamDictionary
    {
        public static class Subtypes
        {
            public const string Form = "Form";
            public const string Image = "Image";
        }

        public XObjectDictionary(Dictionary dict)
            : base(dict) { }

        public XObjectDictionary(Dictionary<Name, IPdfObject> dict, IPdfEditor pdfEditor)
            : base(dict, pdfEditor) { }

        protected XObjectDictionary(
            Name subtype,
            Number length,
            ShorthandArrayObject? filter,
            ShorthandArrayObject? decodeParms,
            Dictionary? f,
            ShorthandArrayObject? fFilter,
            ShorthandArrayObject? fDecodeParms,
            Number? dL,
            IPdfEditor pdfEditor
            )
            : base(
                  Constants.DictionaryTypes.XObject,
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
            ArgumentNullException.ThrowIfNull(subtype);

            Set(Constants.DictionaryKeys.Subtype, subtype);
        }
    }
}
