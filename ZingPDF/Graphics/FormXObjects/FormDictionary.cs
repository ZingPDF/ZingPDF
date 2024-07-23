using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Graphics.FormXObjects
{
    internal abstract class FormDictionary : StreamDictionary
    {
        public static class Subtypes
        {
            public const string Form = "Form";
        }

        protected FormDictionary(Name subtype) : base(Constants.DictionaryTypes.XObject)
        {
            Set(Constants.DictionaryKeys.Subtype, subtype);
        }

        protected FormDictionary(Dictionary dictionary) : base(dictionary)
        {
        }
    }
}
