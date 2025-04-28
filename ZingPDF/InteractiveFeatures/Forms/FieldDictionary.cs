using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.Parsing.Parsers.Objects.Dictionaries;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.InteractiveFeatures.Forms
{
    /// <summary>
    /// ISO 32000-2:2020 12.7.4 - Field dictionaries
    /// </summary>
    public class FieldDictionary : WidgetAnnotationDictionary
    {
        public FieldDictionary(Dictionary dict)
            : base(dict) { }

        private FieldDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
            : base(dictionary, pdfEditor) { }

        /// <summary>
        /// (Required for terminal fields; inheritable)<para></para>
        /// The type of field that this dictionary describes:<para></para>
        /// Btn Button (see 12.7.5.2, "Button fields")<para></para>
        /// Tx Text (see 12.7.5.3, "Text fields")<para></para>
        /// Ch Choice (see 12.7.5.4, "Choice fields")<para></para>
        /// Sig (PDF 1.3) Signature (see 12.7.5.5, "Signature fields")<para></para>
        /// This entry may be present in a non-terminal field (one whose descendants are fields) 
        /// to provide an inheritable FT value. However, a non-terminal field does not logically 
        /// have a type of its own; it is merely a container for inheritable attributes that are 
        /// intended for descendant terminal fields of any type.
        /// </summary>
        [Inheritable]
        public OptionalProperty<Name> FT => GetOptionalProperty<Name>(Constants.DictionaryKeys.Field.FT);

        /// <summary>
        /// (Sometimes required, as described below)<para></para>
        /// An array of indirect references to the immediate children of this field.
        /// In a non-terminal field, the Kids array shall refer to field dictionaries 
        /// that are immediate descendants of this field. In a terminal field, the Kids 
        /// array ordinarily shall refer to one or more separate widget annotations 
        /// that are associated with this field. However, if there is only one associated 
        /// widget annotation, and its contents have been merged into the field dictionary, 
        /// Kids shall be omitted.
        /// </summary>
        public OptionalProperty<ArrayObject> Kids => GetOptionalProperty<ArrayObject>(Constants.DictionaryKeys.Field.Kids);

        /// <summary>
        /// (Optional)<para></para>
        /// The partial field name (see 12.7.4.2, "Field names").
        /// </summary>
        public OptionalProperty<LiteralString> T => GetOptionalProperty<LiteralString>(Constants.DictionaryKeys.Field.T);

        /// <summary>
        /// (Optional; PDF 1.3)<para></para>
        /// An alternative field name that shall be used in place of the actual field name 
        /// wherever the field shall be identified in the user interface 
        /// (such as in error or status messages referring to the field). 
        /// This text is also useful when extracting the document’s contents in support 
        /// of accessibility to users with disabilities or for other purposes (see 14.9.3, "Alternate descriptions").
        /// </summary>
        public OptionalProperty<LiteralString> TU => GetOptionalProperty<LiteralString>(Constants.DictionaryKeys.Field.TU);

        /// <summary>
        /// (Optional; PDF 1.3)<para></para>
        /// The mapping name that shall be used when exporting interactive form field data from the document.
        /// </summary>
        public OptionalProperty<LiteralString> TM => GetOptionalProperty<LiteralString>(Constants.DictionaryKeys.Field.TM);

        /// <summary>
        /// (Optional; inheritable)<para></para>
        /// A set of flags specifying various characteristics of the field 
        /// (see "Table 227 — Field flags common to all field types").<para></para>
        /// Default value: 0.
        /// </summary>
        [Inheritable]
        public OptionalProperty<Number> Ff => GetOptionalProperty<Number>(Constants.DictionaryKeys.Field.Ff);

        /// <summary>
        /// (Optional; inheritable)<para></para>
        /// The field’s value, whose format varies depending on the field type. 
        /// See the descriptions of individual field types for further information.
        /// </summary>
        [Inheritable]
        public OptionalProperty<IPdfObject> V => GetOptionalProperty<IPdfObject>(Constants.DictionaryKeys.Field.V);

        /// <summary>
        /// (Optional; inheritable)<para></para>
        /// The default value to which the field reverts when a reset-form action is executed 
        /// (see 12.7.6.3, "Reset-form action"). The format of this value is the same as that of V.
        /// </summary>
        [Inheritable]
        public OptionalProperty<IPdfObject> DV => GetOptionalProperty<IPdfObject>(Constants.DictionaryKeys.Field.DV);

        /// <summary>
        /// <para>For Button fields (checkboxes/radio buttons):</para>
        /// <para>(Optional; inheritable; PDF 1.4) An array containing one entry for each widget annotation 
        /// in the Kids array of the radio button or check box field. Each entry shall be a text string 
        /// representing the on state of the corresponding widget annotation.</para>
        /// <para>When this entry is present, the names used to represent the on state in the AP dictionary 
        /// of each annotation may use numerical position (starting with 0) of the annotation in the Kids 
        /// array, encoded as a name object (for example: /0, /1). This allows distinguishing between the 
        /// annotations even if two or more of them have the same value in the Opt array.</para>
        /// <para>For Choice fields (list/combo boxes):</para>
        /// <para>(Optional) An array of options that shall be presented to the user. Each element of the array 
        /// is either a text string representing one of the available options or an array consisting of two 
        /// text strings: the option’s export value and the text that shall be displayed as the name of the option.</para>
        /// <para>If this entry is not present, no choices should be presented to the user.</para>
        /// </summary>
        [Inheritable]
        public RequiredProperty<ArrayObject> Opt => GetRequiredProperty<ArrayObject>(Constants.DictionaryKeys.Field.Opt);

        /// <summary>
        /// (Optional) For scrollable list boxes, the top index (the index in the Opt array of the first 
        /// option visible in the list). Default value: 0.
        /// </summary>
        public OptionalProperty<Number> TI => GetOptionalProperty<Number>(Constants.DictionaryKeys.Field.TI);

        /// <summary>
        /// (Required; inheritable) The default appearance string containing a sequence of valid page-content graphics 
        /// or text state operators that define such properties as the field’s text size and colour.
        /// </summary>
        [Inheritable]
        public RequiredProperty<LiteralString> DA => GetRequiredProperty<LiteralString>(Constants.DictionaryKeys.Field.VariableText.DA);

        /// <summary>
        /// (Optional; inheritable) A code specifying the form of quadding (justification) that shall be used in displaying the text:
        /// 0 Left-justified
        /// 1 Centred
        /// 2 Right-justified
        /// Default value: 0 (left-justified).
        /// </summary>
        [Inheritable]
        public OptionalProperty<Number> Q => GetOptionalProperty<Number>(Constants.DictionaryKeys.Field.VariableText.Q);

        /// <summary>
        /// (Optional; PDF 1.5) A default style string, as described in Adobe XML Architecture, XML Forms Architecture (XFA) Specification, version 3.3.
        /// </summary>
        public OptionalProperty<LiteralString> DS => GetOptionalProperty<LiteralString>(Constants.DictionaryKeys.Field.VariableText.DS);

        /// <summary>
        /// (Optional; PDF 1.5) A rich text string, as described in Adobe XML Architecture, XML Forms Architecture (XFA) Specification, version 3.3.
        /// </summary>
        public OptionalMultiProperty<LiteralString, StreamObject<IStreamDictionary>> RV
            => GetOptionalMultiProperty<LiteralString, StreamObject<IStreamDictionary>>(Constants.DictionaryKeys.Field.VariableText.RV);

        public void SetValue(IPdfObject? value)
        {
            Set(Constants.DictionaryKeys.Field.V, value);
        }

        internal async Task SetDefaultAppearanceAsync(ContentStream defaultAppearance)
        {
            ArgumentNullException.ThrowIfNull(defaultAppearance, nameof(defaultAppearance));

            using var ms = new MemoryStream();
            await defaultAppearance.WriteAsync(ms);

            ms.Position = 0;

            Set(Constants.DictionaryKeys.Field.VariableText.DA, new LiteralString(await ms.GetAsync()));
        }

        new public static FieldDictionary FromDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
        {
            return new(dictionary, pdfEditor);
        }
    }
}
