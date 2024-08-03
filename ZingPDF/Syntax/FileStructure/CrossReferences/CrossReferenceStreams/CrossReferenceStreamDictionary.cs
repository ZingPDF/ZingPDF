using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams
{
    internal class CrossReferenceStreamDictionary : StreamDictionary, ITrailerDictionary
    {
        private CrossReferenceStreamDictionary(Dictionary xrefStreamDictionary) : base(xrefStreamDictionary) { }

        /// <summary>
        /// An array containing a pair of integers for each subsection in this section.
        /// The first integer shall be the first object number in the subsection;
        /// the second integer shall be the number of entries in the subsection.<para></para>
        /// The array shall be sorted in ascending order by object number.
        /// Subsections cannot overlap; an object number shall have no more than one entry in a section.
        /// </summary>
        public ArrayObject? Index => Get<ArrayObject>(Constants.DictionaryKeys.CrossReferenceStream.Index);

        /// <summary>
        /// An array of integers representing the size of the fields in a single cross-reference entry.
        /// </summary>
        /// <remarks>
        /// "Table 18 — Entries in a cross-reference stream" describes the types of entries and their fields. 
        /// For PDF 1.5, W always contains three integers; the value of each integer shall be the number of bytes 
        /// (in the decoded stream) of the corresponding field.<para></para>
        ///     EXAMPLE [1 2 1] means that the fields are one byte, two bytes, and one byte, respectively.<para></para>
        /// A value of zero for an element in the W array indicates that the corresponding field shall not be present in the stream, and the default value shall be used, if there is one.
        /// A value of zero shall not be used for the second element of the array. If the first element is zero, the type field shall not be present, and shall default to Type 1.
        /// The sum of the items shall be the total length of each entry; it can be used with the Index array to determine the starting position of each subsection.
        /// Different cross-reference streams in a PDF file may use different values for W.
        /// </remarks>
        public ArrayObject W => Get<ArrayObject>(Constants.DictionaryKeys.CrossReferenceStream.W)!;

        // TODO: see if we can inherit all these properties from a base trailer stream dictionary,
        // rather than duplicating them through interfaces

        #region ITrailerDictionary

        /// <summary>
        /// The number one greater than the highest object number used in this section or in any section for which this shall be an update.
        /// It shall be equivalent to the Size entry in a trailer dictionary.
        /// </summary>
        public Integer Size => Get<Integer>(TrailerDictionary.DictionaryKeys.Size)!;

        /// <summary>
        /// (Required, if any cross reference streams are already present in the file)
        /// The byte offset from the beginning of the PDF file to the beginning of the previous cross-reference stream.
        /// The value is meaningful if the PDF file has more than one cross-reference stream.
        /// It is not meaningful in hybrid-reference files; see 7.5.8.4, "Compatibility with applications that do not support compressed reference streams".
        /// This entry has the same function as the Prev entry in the trailer dictionary
        /// ("Table 15 — Entries in the file trailer dictionary").
        /// </summary>
        public Integer? Prev => Get<Integer>(TrailerDictionary.DictionaryKeys.Prev);

        /// <summary>
        /// The catalog dictionary for the PDF file.
        /// </summary>
        public IndirectObjectReference Root => Get<IndirectObjectReference>(TrailerDictionary.DictionaryKeys.Root)!;

        /// <summary>
        /// Required if document is encrypted; Added in PDF 1.1.
        /// </summary>
        public Dictionary? Encrypt => Get<Dictionary>(TrailerDictionary.DictionaryKeys.Encrypt);

        /// <summary>
        /// Optional. Deprecated in PDF 2.0. The PDF file's information dictionary.<para></para>
        /// N.B. The ModDate key within the Info dictionary is required if Page-Piece dictionaries are used. 
        /// </summary>
        public IndirectObjectReference? Info => Get<IndirectObjectReference>(TrailerDictionary.DictionaryKeys.Info);

        /// <summary>
        /// Required in PDF 2.0 and later, or if an Encrypt entry is present; optional otherwise; Added in PDF 1.1.
        /// </summary>
        public ArrayObject? ID => Get<ArrayObject>(TrailerDictionary.DictionaryKeys.ID);

        #endregion

        public Integer Field1Size => W.Get<Integer>(0)!;
        public Integer Field2Size => W.Get<Integer>(1)!;
        public Integer Field3Size => W.Get<Integer>(2)!;

        public static CrossReferenceStreamDictionary CreateNew(
            ArrayObject index,
            ArrayObject w,
            Integer size,
            Integer? prev,
            IndirectObjectReference root,
            Dictionary? encrypt,
            IndirectObjectReference? info,
            ArrayObject? id
            )
        {
            ArgumentNullException.ThrowIfNull(index);
            ArgumentNullException.ThrowIfNull(w);
            ArgumentNullException.ThrowIfNull(size);
            ArgumentNullException.ThrowIfNull(root);

            var dict = new Dictionary<Name, IPdfObject>
            {
                { Constants.DictionaryKeys.Type, new Name(Constants.DictionaryTypes.XRef) },
                { Constants.DictionaryKeys.CrossReferenceStream.Index, index },
                { Constants.DictionaryKeys.CrossReferenceStream.W, w },
                { TrailerDictionary.DictionaryKeys.Size, size },
                { TrailerDictionary.DictionaryKeys.Root, root },
            };

            if (prev is not null)
            {
                dict[TrailerDictionary.DictionaryKeys.Prev] = prev;
            }

            if (encrypt is not null)
            {
                dict[TrailerDictionary.DictionaryKeys.Encrypt] = encrypt;
            }

            if (info is not null)
            {
                dict[TrailerDictionary.DictionaryKeys.Info] = info;
            }

            if (id is not null)
            {
                dict[TrailerDictionary.DictionaryKeys.ID] = id;
            }

            return new(dict);
        }

        new public static CrossReferenceStreamDictionary FromDictionary(Dictionary xrefStreamDictionary)
        {
            ArgumentNullException.ThrowIfNull(xrefStreamDictionary);

            if (!xrefStreamDictionary.TryGetValue(Constants.DictionaryKeys.Type, out IPdfObject? type) || (Name)type != Constants.DictionaryTypes.XRef)
            {
                throw new ArgumentException("Supplied argument is not a cross reference stream dictionary.", nameof(xrefStreamDictionary));
            }

            return new(xrefStreamDictionary);
        }
    }
}
