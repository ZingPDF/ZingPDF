using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.InteractiveFeatures.Forms
{
    /// <summary>
    /// ISO 32000-2:2020 12.7.3 - Interactive form dictionary
    /// </summary>
    public class InteractiveFormDictionary : Dictionary
    {
        public InteractiveFormDictionary(Dictionary dictionary)
            : base(dictionary) { }

        private InteractiveFormDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
            : base(dictionary, pdfEditor) { }

        /// <summary>
        /// (Required) An array of references to the document’s root fields (those with no ancestors in the field hierarchy).
        /// </summary>
        public RequiredProperty<ArrayObject> Fields => GetRequiredProperty<ArrayObject>(Constants.DictionaryKeys.InteractiveForm.Fields);

        /// <summary>
        /// (Optional; deprecated in PDF 2.0)<para></para>
        /// A flag specifying whether to construct appearance streams and appearance dictionaries for all 
        /// widget annotations in the document (see 12.7.4.3, "Variable text").<para></para> 
        /// Default value: false. A PDF writer shall include this key, with a value of true, 
        /// if it has not provided appearance streams for all visible widget annotations present in the document.
        /// NOTE Appearance streams are required in PDF 2.0 and later.
        /// </summary>
        public OptionalProperty<BooleanObject> NeedAppearances => GetOptionalProperty<BooleanObject>(Constants.DictionaryKeys.InteractiveForm.NeedAppearances);

        /// <summary>
        /// <para>
        /// (Optional; PDF 1.3) A set of flags specifying various document-level characteristics related to signature fields 
        /// (see "Table 225 — Signature flags", and 12.7.5.5, "Signature fields").
        /// </para>
        /// <para>Default value: 0.</para>
        /// </summary>
        public OptionalProperty<Number> SigFlags => GetOptionalProperty<Number>(Constants.DictionaryKeys.InteractiveForm.SigFlags);

        /// <summary>
        /// <para>(Required if any fields in the document have additional-actions dictionaries containing a C entry; PDF 1.3)</para>
        /// <para>
        /// An array of indirect references to field dictionaries with calculation actions, defining the calculation 
        /// order in which their values will be recalculated when the value of any field changes (see 12.6.3, "Trigger events").
        /// </para>
        /// </summary>
        public OptionalProperty<ArrayObject> CO => GetOptionalProperty<ArrayObject>(Constants.DictionaryKeys.InteractiveForm.CO);

        /// <summary>
        /// (Optional) A resource dictionary (see 7.8.3, "Resource dictionaries") containing default resources 
        /// (such as fonts, patterns, or colour spaces) that shall be used by form field appearance streams. 
        /// At a minimum, this dictionary shall contain a Font entry specifying the resource name and font 
        /// dictionary of the default font for displaying text.
        /// </summary>
        // TODO: Is there a way to parse this as a proper ResourceDictionary?
        public OptionalProperty<Dictionary> DR => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.InteractiveForm.DR);

        /// <summary>
        /// (Optional) A document-wide default value for the DA attribute of variable text fields (see 12.7.4.3, "Variable text").
        /// </summary>
        public OptionalProperty<LiteralString> DA => GetOptionalProperty<LiteralString>(Constants.DictionaryKeys.InteractiveForm.DA);

        /// <summary>
        /// (Optional) A document-wide default value for the Q attribute of variable text fields (see 12.7.4.3, "Variable text").
        /// </summary>
        public OptionalProperty<Number> Q => GetOptionalProperty<Number>(Constants.DictionaryKeys.InteractiveForm.Q);

        /// <summary>
        /// <para>
        /// (Optional; deprecated in PDF 2.0) A stream or array containing an XFA resource, whose format shall conform to 
        /// the Data Package (XDP) Specification.
        /// </para>
        /// <para>See Annex K, “XFA forms”.</para>
        /// </summary>
        public OptionalProperty<IPdfObject> XFA => GetOptionalProperty<IPdfObject>(Constants.DictionaryKeys.InteractiveForm.XFA);

        public void SetNeedAppearances(BooleanObject needAppearances)
        {
            ArgumentNullException.ThrowIfNull(needAppearances);

            Set(Constants.DictionaryKeys.InteractiveForm.NeedAppearances, needAppearances);
        }

        public void SetResources(ResourceDictionary resources)
        {
            ArgumentNullException.ThrowIfNull(resources);

            Set(Constants.DictionaryKeys.InteractiveForm.DR, resources);
        }

        public static InteractiveFormDictionary FromDictionary(Dictionary<Name, IPdfObject> dict, IPdfEditor pdfEditor)
        {
            return new(dict, pdfEditor);
        }
    }
}
