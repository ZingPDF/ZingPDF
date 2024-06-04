using ZingPDF.ObjectModel;
using ZingPDF.ObjectModel.Objects;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF.Forms
{
    internal class FieldDictionary : Dictionary
    {
        public static class DictionaryKeys
        {
            public const string FT = "FT";
            public const string Parent = "Parent";
            public const string Kids = "Kids";
            public const string T = "T";
            public const string TU = "TU";
            public const string TM = "TM";
            public const string Ff = "Ff";
            public const string V = "V";
        }

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
        public Name? FT { get => Get<Name>(DictionaryKeys.FT); }

        /// <summary>
        /// (Required if this field is the child of another in the field hierarchy; absent otherwise)<para></para>
        /// The field that is the immediate parent of this one (the field, if any, whose Kids array includes this field). 
        /// A field can have at most one parent; that is, it can be included in the Kids array of at most one other field.
        /// </summary>
        public IndirectObjectReference? Parent { get => Get<IndirectObjectReference>(DictionaryKeys.Parent); }

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
        public ArrayObject? Kids { get => Get<ArrayObject>(DictionaryKeys.Kids); }

        /// <summary>
        /// (Optional)<para></para>
        /// The partial field name (see 12.7.4.2, "Field names").
        /// </summary>
        public LiteralString? T { get => Get<LiteralString>(DictionaryKeys.T); }

        /// <summary>
        /// (Optional; PDF 1.3)<para></para>
        /// An alternative field name that shall be used in place of the actual field name 
        /// wherever the field shall be identified in the user interface 
        /// (such as in error or status messages referring to the field). 
        /// This text is also useful when extracting the document’s contents in support 
        /// of accessibility to users with disabilities or for other purposes (see 14.9.3, "Alternate descriptions").
        /// </summary>
        public LiteralString? TU { get => Get<LiteralString>(DictionaryKeys.TU); }

        /// <summary>
        /// (Optional; PDF 1.3)<para></para>
        /// The mapping name that shall be used when exporting interactive form field data from the document.
        /// </summary>
        public LiteralString? TM { get => Get<LiteralString>(DictionaryKeys.TM); }

        /// <summary>
        /// (Optional; inheritable)<para></para>
        /// A set of flags specifying various characteristics of the field 
        /// (see "Table 227 — Field flags common to all field types").<para></para>
        /// Default value: 0.
        /// </summary>
        public Integer? Ff { get => Get<Integer>(DictionaryKeys.Ff); }

        /// <summary>
        /// (Optional; inheritable)<para></para>
        /// The field’s value, whose format varies depending on the field type. 
        /// See the descriptions of individual field types for further information.
        /// </summary>
        public IPdfObject? V { get => Get<IPdfObject>(DictionaryKeys.V); }


    }
}
