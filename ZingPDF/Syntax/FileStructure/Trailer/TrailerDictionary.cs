using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.FileStructure.Trailer
{
    public class TrailerDictionary : Dictionary, ITrailerDictionary
    {
        public static class DictionaryKeys
        {
            public const string Size = "Size";
            public const string Prev = "Prev";
            public const string Root = "Root";
            public const string Encrypt = "Encrypt";
            public const string Info = "Info";
            public const string ID = "ID";
        }

        private TrailerDictionary(Dictionary trailerDictionary) : base(trailerDictionary) { }

        /// <summary>
        /// The total number of entries in the PDF file's cross-reference table, as defined by the combination of the original section and all update sections.
        /// Equivalently, this value shall be 1 greater than the highest object number defined in the PDF file.
        /// </summary>
        public Integer Size { get => Get<Integer>(DictionaryKeys.Size)!; }

        /// <summary>
        /// Optional, present only if the file has more than one cross-reference section;
        /// The byte offset from the beginning of the PDF file to the beginning of the previous cross-reference section.
        /// </summary>
        public Integer? Prev { get => Get<Integer>(DictionaryKeys.Prev); }

        /// <summary>
        /// The catalog dictionary for the PDF file.
        /// </summary>
        public IndirectObjectReference? Root { get => Get<IndirectObjectReference>(DictionaryKeys.Root); }

        /// <summary>
        /// Required if document is encrypted; Added in PDF 1.1.
        /// </summary>
        public Dictionary? Encrypt { get => Get<Dictionary>(DictionaryKeys.Encrypt); }

        /// <summary>
        /// Optional. Deprecated in PDF 2.0. The PDF file's information dictionary.<para></para>
        /// N.B. The ModDate key within the Info dictionary is required if Page-Piece dictionaries are used. 
        /// </summary>
        public IndirectObjectReference? Info { get => Get<IndirectObjectReference>(DictionaryKeys.Info); }

        /// <summary>
        /// Required in PDF 2.0 and later, or if an Encrypt entry is present; optional otherwise; Added in PDF 1.1.
        /// </summary>
        public ArrayObject? ID { get => Get<ArrayObject>(DictionaryKeys.ID); }

        /// <summary>
        /// Create a page from an existing page dictionary.
        /// </summary>
        /// <param name="trailerDictionary">An existing dictionary from which to create the <see cref="TrailerDictionary"/>.</param>
        /// <returns>A <see cref="TrailerDictionary"/> instance.</returns>
        internal static TrailerDictionary FromDictionary(Dictionary trailerDictionary)
        {
            ArgumentNullException.ThrowIfNull(trailerDictionary);

            if (trailerDictionary.Get<Integer>(DictionaryKeys.Size) is null)
            {
                throw new ArgumentException($"Missing required {DictionaryKeys.Size} entry in {trailerDictionary}", nameof(trailerDictionary));
            }

            return new TrailerDictionary(trailerDictionary);
        }

        /// <summary>
        /// Create a new <see cref="TrailerDictionary"/>.
        /// </summary>
        internal static TrailerDictionary CreateNew(
            Integer size,
            Integer? prev,
            IndirectObjectReference root,
            Dictionary? encrypt,
            IndirectObjectReference? info,
            ArrayObject? id
            )
        {
            ArgumentNullException.ThrowIfNull(size);
            ArgumentNullException.ThrowIfNull(root);

            var dict = new Dictionary<Name, IPdfObject>
            {
                { DictionaryKeys.Size, size },
                { DictionaryKeys.Root, root },
            };

            if (prev != null)
            {
                dict.Add(DictionaryKeys.Prev, prev);
            }

            if (encrypt != null)
            {
                dict.Add(DictionaryKeys.Encrypt, encrypt);
            }

            if (info != null)
            {
                dict.Add(DictionaryKeys.Info, info);
            }

            if (id != null)
            {
                dict.Add(DictionaryKeys.ID, id);
            }

            return new(dict);
        }
    }
}
