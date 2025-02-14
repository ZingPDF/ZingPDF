using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Text.SimpleFonts
{
    internal abstract class FontDictionary : Dictionary
    {
        protected static class Subtypes
        {
            public const string Type1 = "Type1";
        }

        public FontDictionary(Name subType) : base(Constants.DictionaryTypes.Font)
        {
            ArgumentNullException.ThrowIfNull(subType);

            Set(Constants.DictionaryKeys.Subtype, subType);
        }
    }
}
