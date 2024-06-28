using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.Linearization
{
    /// <summary>
    /// ISO 32000-2:2020 F.3.3 - Linearization parameter dictionary
    /// </summary
    public class LinearizationParameterDictionary : Dictionary
    {
        public static class DictionaryKeys
        {
            public const string Linearized = "Linearized";
            public const string L = "L";
            public const string H = "H";
            public const string O = "O";
            public const string E = "E";
            public const string N = "N";
            public const string T = "T";
            public const string P = "P";
        }

        private LinearizationParameterDictionary(Dictionary linearizationDictionary) : base(linearizationDictionary) { }

        /// <summary>
        /// A version identification for the linearized format.
        /// </summary>
        public RealNumber Linearized => Get<RealNumber>(DictionaryKeys.Linearized)!;

        /// <summary>
        /// The length of the entire PDF file in bytes.
        /// </summary>
        /// <remarks>
        /// It shall be exactly equal to the actual length of the PDF file.
        /// A mismatch indicates that the file is not linearized and shall be treated as ordinary PDF file, ignoring linearization information.
        /// (If the mismatch resulted from appending an update, the linearization information may still be correct but requires validation;
        /// </remarks>
        public Integer L => Get<Integer>(DictionaryKeys.L)!;

        /// <summary>
        /// The offset of the primary hint stream from the beginning of the PDF file.
        /// </summary>
        /// <remarks>
        /// An array of two or four integers, [offset1 length1] or [offset1 length1 offset2 length2].
        /// offset1 shall be the offset of the primary hint stream from the beginning of the PDF file.
        /// (This is the beginning of the stream object, not the beginning of the stream data.)
        /// length1 shall be the length of this stream, including stream object overhead.<para></para>
        /// If the value of the primary hint stream dictionary's Length entry is an indirect reference,
        /// the object it refers to shall immediately follow the stream object,
        /// and length1 also shall include the length of the indirect length object, including object overhead.<para></para>
        /// If there is an overflow hint stream, offset2 and length2 shall specify its offset and length.
        /// </remarks>
        public ArrayObject H => Get<ArrayObject>(DictionaryKeys.H)!;

        /// <summary>
        /// The object number of the first page's page object.
        /// </summary>
        public Integer O => Get<Integer>(DictionaryKeys.O)!;

        /// <summary>
        /// The offset of the end of the first page, relative to the beginning of the PDF file.
        /// </summary>
        public Integer E => Get<Integer>(DictionaryKeys.E)!;

        /// <summary>
        /// The number of pages in the document.
        /// </summary>
        public Integer N => Get<Integer>(DictionaryKeys.N)!;

        /// <summary>
        /// In documents that use standard main cross-reference tables (including hybrid-reference files),
        /// this entry shall represent the offset of the white-space character preceding the first entry
        /// of the main cross-reference table (the entry for object number 0), relative to the beginning of the PDF file.
        /// Note that this differs from the Prev entry in the first-page trailer, which gives the location of the xref line that precedes the table.<para></para>
        /// (PDF 1.5) Documents that use cross-reference streams exclusively,
        /// this entry shall represent the offset of the main cross-reference stream object in the PDF file.
        /// </summary>
        public Integer T => Get<Integer>(DictionaryKeys.T)!;

        /// <summary>
        /// The page number of the first page.
        /// </summary>
        public Integer? P => Get<Integer>(DictionaryKeys.P);

        public static LinearizationParameterDictionary FromDictionary(Dictionary linearizationDictionary)
        {
            ArgumentNullException.ThrowIfNull(linearizationDictionary);

            return new(linearizationDictionary);
        }
    }
}
