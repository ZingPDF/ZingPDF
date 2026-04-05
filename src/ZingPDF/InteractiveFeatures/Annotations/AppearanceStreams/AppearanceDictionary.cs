using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams
{
    public class AppearanceDictionary : Dictionary
    {
        public AppearanceDictionary(Dictionary dictionary)
            : base(dictionary) { }

        private AppearanceDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
            : base(dictionary, pdf, context) { }

        /// <summary>
        /// (Required) The annotation’s normal appearance.
        /// </summary>
        public RequiredMultiProperty<IStreamObject, Dictionary> N
            => GetRequiredMultiProperty<IStreamObject, Dictionary>(Constants.DictionaryKeys.Appearance.N);

        /// <summary>
        /// (Optional) The annotation’s rollover appearance. Default value: the value of the N entry.
        /// </summary>
        public OptionalMultiProperty<IStreamObject, Dictionary> R
            => GetOptionalMultiProperty<IStreamObject, Dictionary>(Constants.DictionaryKeys.Appearance.R);

        /// <summary>
        /// (Optional) The annotation’s down appearance. Default value: the value of the N entry.
        /// </summary>
        public OptionalMultiProperty<IStreamObject, Dictionary> D
            => GetOptionalMultiProperty<IStreamObject, Dictionary>(Constants.DictionaryKeys.Appearance.D);

        public static AppearanceDictionary Create(
            IPdf pdf,
            ObjectContext context,
            IndirectObjectReference normalAppearanceStream,
            IndirectObjectReference? rolloverAppearanceStream = null,
            IndirectObjectReference? downAppearanceStream = null
            )
        {
            var dict = new Dictionary<string, IPdfObject> {{ Constants.DictionaryKeys.Appearance.N, normalAppearanceStream }};

            if (rolloverAppearanceStream is not null)
            {
                dict.Add(Constants.DictionaryKeys.Appearance.R, rolloverAppearanceStream);
            }

            if (downAppearanceStream is not null)
            {
                dict.Add(Constants.DictionaryKeys.Appearance.D, downAppearanceStream);
            }

            return new AppearanceDictionary(dict, pdf, context);
        }

        public static AppearanceDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
        {
            return new AppearanceDictionary(dictionary, pdf, context);
        }
    }
}
