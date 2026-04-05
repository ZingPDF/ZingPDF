using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Linearization
{
    /// <summary>
    /// ISO 32000-2:2020 F.3.3 - Linearization parameter dictionary
    /// </summary>
    public class LinearizationParameterDictionary : Dictionary
    {
        public LinearizationParameterDictionary(Dictionary linearizationDictionary)
            : base(linearizationDictionary) { }

        private LinearizationParameterDictionary(Dictionary<string, IPdfObject> linearizationDictionary, IPdf pdf, ObjectContext context)
            : base(linearizationDictionary, pdf, context) { }

        /// <summary>
        /// (Required) A version identification for the linearized format.
        /// </summary>
        public Number Linearized => GetAs<Number>(Constants.DictionaryKeys.LinearizationParameter.Linearized);

        /// <summary>
        /// (Required) The length of the entire PDF file in bytes. It shall be exactly equal to the actual length of the PDF file.
        /// A mismatch indicates that the file is not linearized and shall be treated as ordinary PDF file, ignoring linearization information.
        /// (If the mismatch resulted from appending an update, the linearization information may still be correct but requires validation;
        /// </summary>
        public Number L => GetAs<Number>(Constants.DictionaryKeys.LinearizationParameter.L);

        /// <summary>
        /// <para>
        /// (Required) An array of two or four integers, [offset1 length1] or [offset1 length1 offset2 length2]. 
        /// offset1 shall be the offset of the primary hint stream from the beginning of the PDF file. (This is the 
        /// beginning of the stream object, not the beginning of the stream data.) length1 shall be the length of this 
        /// stream, including stream object overhead.
        /// </para>
        /// <para>
        /// If the value of the primary hint stream dictionary’s Length entry is an indirect reference, the 
        /// object it refers to shall immediately follow the stream object, and length1 also shall include the length 
        /// of the indirect length object, including object overhead.
        /// </para>
        /// <para>If there is an overflow hint stream, offset2 and length2 shall specify its offset and length.</para>
        /// </summary>
        public ArrayObject H => GetAs<ArrayObject>(Constants.DictionaryKeys.LinearizationParameter.H);

        /// <summary>
        /// (Required) The object number of the first page's page object.
        /// </summary>
        public Number O => GetAs<Number>(Constants.DictionaryKeys.LinearizationParameter.O);

        /// <summary>
        /// (Required) The offset of the end of the first page (the end of Example 6 in F.3, "Linearized PDF document 
        /// structure"), relative to the beginning of the PDF file.
        /// </summary>
        public Number E => GetAs<Number>(Constants.DictionaryKeys.LinearizationParameter.E);

        /// <summary>
        /// (Required) The number of pages in the document.
        /// </summary>
        public Number N => GetAs<Number>(Constants.DictionaryKeys.LinearizationParameter.N);

        /// <summary>
        /// <para>
        /// (Required) In documents that use standard main cross-reference tables (including hybrid-reference files; 
        /// see 7.5.8.4, "Compatibility with applications that do not support compressed reference streams"), this 
        /// entry shall represent the offset of the white-space character preceding the first entry of the main 
        /// cross-reference table (the entry for object number 0), relative to the beginning of the PDF file. Note 
        /// that this differs from the Prev entry in the first-page trailer, which gives the location of the xref 
        /// line that precedes the table.
        /// </para>
        /// <para>
        /// (PDF 1.5) Documents that use cross-reference streams exclusively (see 7.5.8, "Cross-reference streams"), 
        /// this entry shall represent the offset of the main cross-reference stream object in the PDF file.
        /// </para>
        /// </summary>
        public Number T => GetAs<Number>(Constants.DictionaryKeys.LinearizationParameter.T);

        /// <summary>
        /// (Optional) The page number of the first page; see F.3.4, "First-page cross-reference table and 
        /// trailer (Part 3)". Default value: 0.
        /// </summary>
        public Number? P => GetAs<Number>(Constants.DictionaryKeys.LinearizationParameter.P);

        public static LinearizationParameterDictionary FromDictionary(Dictionary<string, IPdfObject> linearizationDictionary, IPdf pdf, ObjectContext context)
        {
            ArgumentNullException.ThrowIfNull(linearizationDictionary);

            return new(linearizationDictionary, pdf, context);
        }
    }
}
