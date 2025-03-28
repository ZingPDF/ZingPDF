using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams
{
    public class CrossReferenceStreamDictionary : StreamDictionary, ITrailerDictionary
    {
        private CrossReferenceStreamDictionary(Dictionary<Name, IPdfObject> xrefStreamDictionary)
            : base(xrefStreamDictionary, null) { }

        /// <summary>
        /// <para>
        /// An array containing a pair of integers for each subsection in this section.
        /// The first integer shall be the first object number in the subsection;
        /// the second integer shall be the number of entries in the subsection.
        /// </para>
        /// <para>
        /// The array shall be sorted in ascending order by object number.
        /// Subsections cannot overlap; an object number shall have no more than one entry in a section.
        /// </para>
        /// </summary>
        public ArrayObject? Index => GetAs<ArrayObject>(Constants.DictionaryKeys.CrossReferenceStream.Index);

        /// <summary>
        /// <para>
        /// (Required) An array of integers representing the size of the fields in a single cross-reference entry. 
        /// "Table 18 — Entries in a cross-reference stream" describes the types of entries and their fields. For PDF 1.5, 
        /// W always contains three integers; the value of each integer shall be the number of bytes (in the decoded stream) 
        /// of the corresponding field.</para>
        /// <para>
        /// EXAMPLE [1 2 1] means that the fields are one byte, two bytes, and one byte, respectively.
        /// </para>
        /// <para>
        /// A value of zero for an element in the W array indicates that the corresponding field shall not be present 
        /// in the stream, and the default value shall be used, if there is one. A value of zero shall not be used for the 
        /// second element of the array. If the first element is zero, the type field shall not be present, and shall 
        /// default to Type 1.
        /// </para>
        /// <para>
        /// The sum of the items shall be the total length of each entry; it can be used with the Index array to determine 
        /// the starting position of each subsection.
        /// </para>
        /// <para>
        /// Different cross-reference streams in a PDF file may use different values for W.
        /// </para>
        /// </summary>
        public ArrayObject W => GetAs<ArrayObject>(Constants.DictionaryKeys.CrossReferenceStream.W)!;

        // TODO: see if we can inherit all these properties from a base trailer stream dictionary,
        // rather than duplicating them through interfaces

        #region ITrailerDictionary

        public Number Size => (Number)this[Constants.DictionaryKeys.Trailer.Size];
        public Number? Prev => GetAs<Number>(Constants.DictionaryKeys.Trailer.Prev);
        public IndirectObjectReference? Root => GetAs<IndirectObjectReference>(Constants.DictionaryKeys.Trailer.Root);
        public IndirectObjectReference? Encrypt => GetAs<IndirectObjectReference>(Constants.DictionaryKeys.Trailer.Encrypt);
        public IndirectObjectReference? Info => GetAs<IndirectObjectReference>(Constants.DictionaryKeys.Trailer.Info);
        public ArrayObject? ID => GetAs<ArrayObject>(Constants.DictionaryKeys.Trailer.ID);

        #endregion

        public Number Field1Size => W.Get<Number>(0)!;
        public Number Field2Size => W.Get<Number>(1)!;
        public Number Field3Size => W.Get<Number>(2)!;

        public static CrossReferenceStreamDictionary CreateNew(
            ArrayObject index,
            ArrayObject w,
            Number size,
            Number? prev,
            IndirectObjectReference root,
            IPdfObject? encrypt,
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
                { Constants.DictionaryKeys.Trailer.Size, size },
                { Constants.DictionaryKeys.Trailer.Root, root },
            };

            if (prev is not null)
            {
                dict[Constants.DictionaryKeys.Trailer.Prev] = prev;
            }

            if (encrypt is not null)
            {
                dict[Constants.DictionaryKeys.Trailer.Encrypt] = encrypt;
            }

            if (info is not null)
            {
                dict[Constants.DictionaryKeys.Trailer.Info] = info;
            }

            if (id is not null)
            {
                dict[Constants.DictionaryKeys.Trailer.ID] = id;
            }

            return new(dict);
        }

        public static CrossReferenceStreamDictionary FromDictionary(Dictionary<Name, IPdfObject> xrefStreamDictionary)
        {
            if (!xrefStreamDictionary.TryGetValue(Constants.DictionaryKeys.Type, out IPdfObject? type) || (Name)type != Constants.DictionaryTypes.XRef)
            {
                throw new ArgumentException("Supplied argument is not a cross reference stream dictionary.", nameof(xrefStreamDictionary));
            }

            return new(xrefStreamDictionary);
        }

        internal void SetSize(Number size)
        {
            Set(Constants.DictionaryKeys.Trailer.Size, size);
        }
    }
}
