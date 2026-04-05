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

        public XObjectDictionary(Dictionary<string, IPdfObject> dict, IPdf pdf, ObjectContext context)
            : base(dict, pdf, context) { }
    }
}
