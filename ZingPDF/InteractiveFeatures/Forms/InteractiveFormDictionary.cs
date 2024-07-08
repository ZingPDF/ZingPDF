using ZingPDF.ObjectModel;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.InteractiveFeatures.Forms
{
    /// <summary>
    /// ISO 32000-2:2020 12.7.3 - Interactive form dictionary
    /// </summary>
    public class InteractiveFormDictionary : Dictionary
    {
        private InteractiveFormDictionary(Dictionary dict) : base(dict) { }

        /// <summary>
        /// (Required)<para></para>
        /// An array of references to the document’s root fields (those with no ancestors in the field hierarchy).
        /// </summary>
        public ArrayObject Fields { get => Get<ArrayObject>(Constants.DictionaryKeys.InteractiveForm.Fields)!; }

        /// <summary>
        /// (Optional; deprecated in PDF 2.0)<para></para>
        /// A flag specifying whether to construct appearance streams and appearance dictionaries for all 
        /// widget annotations in the document (see 12.7.4.3, "Variable text").<para></para> 
        /// Default value: false. A PDF writer shall include this key, with a value of true, 
        /// if it has not provided appearance streams for all visible widget annotations present in the document.
        /// NOTE Appearance streams are required in PDF 2.0 and later.
        /// </summary>
        public BooleanObject? NeedAppearances { get => Get<BooleanObject>(Constants.DictionaryKeys.InteractiveForm.NeedAppearances); }

        /// <summary>
        /// (Optional; PDF 1.3)<para></para>
        /// A set of flags specifying various document-level characteristics related to signature fields 
        /// (see "Table 225 — Signature flags", and 12.7.5.5, "Signature fields").<para></para>
        /// Default value: 0.
        /// </summary>
        public Integer? SigFlags { get => Get<Integer>(Constants.DictionaryKeys.InteractiveForm.SigFlags); }

        /// <summary>
        /// (Required if any fields in the document have additional-actions dictionaries containing a C entry; PDF 1.3)<para></para>
        /// An array of indirect references to field dictionaries with calculation actions, defining the calculation 
        /// order in which their values will be recalculated when the value of any field changes (see 12.6.3, "Trigger events").
        /// </summary>
        public ArrayObject? CO { get => Get<ArrayObject>(Constants.DictionaryKeys.InteractiveForm.CO); }

        /// <summary>
        /// (Optional)<para></para>
        /// A resource dictionary (see 7.8.3, "Resource dictionaries") containing default resources 
        /// (such as fonts, patterns, or colour spaces) that shall be used by form field appearance streams. 
        /// At a minimum, this dictionary shall contain a Font entry specifying the resource name and font 
        /// dictionary of the default font for displaying text.
        /// </summary>
        public Dictionary? DR { get => Get<Dictionary>(Constants.DictionaryKeys.InteractiveForm.DR); }

        /// <summary>
        /// (Optional)<para></para>
        /// A document-wide default value for the DA attribute of variable text fields (see 12.7.4.3, "Variable text").
        /// </summary>
        public LiteralString? DA { get => Get<LiteralString>(Constants.DictionaryKeys.InteractiveForm.DA); }

        /// <summary>
        /// (Optional) <para></para>
        /// A document-wide default value for the Q attribute of variable text fields (see 12.7.4.3, "Variable text").
        /// </summary>
        public Integer? Q { get => Get<Integer>(Constants.DictionaryKeys.InteractiveForm.Q); }

        /// <summary>
        /// (Optional; deprecated in PDF 2.0)<para></para>
        /// A stream or array containing an XFA resource, whose format shall conform to the Data Package (XDP) Specification.<para></para>
        /// See Annex K, “XFA forms”.
        /// </summary>
        public IPdfObject? XFA { get => Get<IPdfObject>(Constants.DictionaryKeys.InteractiveForm.XFA); }

        public static InteractiveFormDictionary FromDictionary(Dictionary dict)
        {
            ArgumentNullException.ThrowIfNull(dict);

            return new(dict);
        }
    }
}
