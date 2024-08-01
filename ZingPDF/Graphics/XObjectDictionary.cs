using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Graphics
{
    internal abstract class XObjectDictionary : StreamDictionary
    {
        protected static class Subtypes
        {
            public const string Form = "Form";
            public const string Image = "Image";
        }

        protected XObjectDictionary(Name subtype)
            : base(Constants.DictionaryTypes.XObject)
        {
            ArgumentNullException.ThrowIfNull(subtype);

            Set(Constants.DictionaryKeys.Subtype, subtype);
        }
        
        protected XObjectDictionary(Dictionary streamDictionary) : base(streamDictionary) { }
    }
}
