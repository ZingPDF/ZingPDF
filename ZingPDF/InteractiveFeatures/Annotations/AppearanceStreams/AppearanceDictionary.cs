using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams
{
    public class AppearanceDictionary : Dictionary
    {
        private AppearanceDictionary(Dictionary dictionary) : base(dictionary) { }

        /// <summary>
        /// (Required) The annotation’s normal appearance.
        /// </summary>
        public IPdfObject N => Get<IPdfObject>(Constants.DictionaryKeys.Appearance.N)!;

        /// <summary>
        /// (Optional) The annotation’s rollover appearance. Default value: the value of the N entry.
        /// </summary>
        public IPdfObject? R => Get<IPdfObject>(Constants.DictionaryKeys.Appearance.R);

        /// <summary>
        /// (Optional) The annotation’s down appearance. Default value: the value of the N entry.
        /// </summary>
        public IPdfObject? D => Get<IPdfObject>(Constants.DictionaryKeys.Appearance.D);

        public static AppearanceDictionary Create(
            IndirectObjectReference normalAppearanceStream,
            IndirectObjectReference? rolloverAppearanceStream = null,
            IndirectObjectReference? downAppearanceStream = null
            )
        {
            var dict = new Dictionary<Name, IPdfObject> {{ Constants.DictionaryKeys.Appearance.N, normalAppearanceStream }};

            if (rolloverAppearanceStream is not null)
            {
                dict.Add(Constants.DictionaryKeys.Appearance.R, rolloverAppearanceStream);
            }

            if (downAppearanceStream is not null)
            {
                dict.Add(Constants.DictionaryKeys.Appearance.D, downAppearanceStream);
            }

            return new AppearanceDictionary(dict);
        }

        public static AppearanceDictionary FromDictionary(Dictionary dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            return new AppearanceDictionary(dictionary);
        }
    }
}
