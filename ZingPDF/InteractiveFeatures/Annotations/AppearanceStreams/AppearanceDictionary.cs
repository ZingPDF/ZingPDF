using ZingPDF.IncrementalUpdates;
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

        private AppearanceDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
            : base(dictionary, pdfEditor) { }

        /// <summary>
        /// (Required) The annotation’s normal appearance.
        /// </summary>
        public DictionaryMultiProperty<StreamObject<IStreamDictionary>, Dictionary> N
            => Get<StreamObject<IStreamDictionary>, Dictionary>(Constants.DictionaryKeys.Appearance.N);

        /// <summary>
        /// (Optional) The annotation’s rollover appearance. Default value: the value of the N entry.
        /// </summary>
        public DictionaryMultiProperty<StreamObject<IStreamDictionary>?, Dictionary?> R
            => Get<StreamObject<IStreamDictionary>?, Dictionary?>(Constants.DictionaryKeys.Appearance.R);

        /// <summary>
        /// (Optional) The annotation’s down appearance. Default value: the value of the N entry.
        /// </summary>
        public DictionaryMultiProperty<StreamObject<IStreamDictionary>?, Dictionary?> D
            => Get<StreamObject<IStreamDictionary>?, Dictionary?>(Constants.DictionaryKeys.Appearance.D);

        public static AppearanceDictionary Create(
            IPdfEditor pdfEditor,
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

            return new AppearanceDictionary(dict, pdfEditor);
        }

        public static AppearanceDictionary FromDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
        {
            return new AppearanceDictionary(dictionary, pdfEditor);
        }
    }
}
