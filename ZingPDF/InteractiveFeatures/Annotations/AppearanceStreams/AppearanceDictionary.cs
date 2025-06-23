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

        private AppearanceDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectOrigin objectOrigin)
            : base(dictionary, pdf, objectOrigin) { }

        /// <summary>
        /// (Required) The annotation’s normal appearance.
        /// </summary>
        public RequiredMultiProperty<StreamObject<IStreamDictionary>, Dictionary> N
            => GetRequiredMultiProperty<StreamObject<IStreamDictionary>, Dictionary>(Constants.DictionaryKeys.Appearance.N);

        /// <summary>
        /// (Optional) The annotation’s rollover appearance. Default value: the value of the N entry.
        /// </summary>
        public OptionalMultiProperty<StreamObject<IStreamDictionary>, Dictionary> R
            => GetOptionalMultiProperty<StreamObject<IStreamDictionary>, Dictionary>(Constants.DictionaryKeys.Appearance.R);

        /// <summary>
        /// (Optional) The annotation’s down appearance. Default value: the value of the N entry.
        /// </summary>
        public OptionalMultiProperty<StreamObject<IStreamDictionary>, Dictionary> D
            => GetOptionalMultiProperty<StreamObject<IStreamDictionary>, Dictionary>(Constants.DictionaryKeys.Appearance.D);

        public static AppearanceDictionary Create(
            IPdf pdf,
            ObjectOrigin objectOrigin,
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

            return new AppearanceDictionary(dict, pdf, objectOrigin);
        }

        public static AppearanceDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectOrigin objectOrigin)
        {
            return new AppearanceDictionary(dictionary, pdf, objectOrigin);
        }
    }
}
