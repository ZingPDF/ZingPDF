using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Objects.ObjectGroups.Trailer
{
    internal class TrailerDictionary : Dictionary
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

        private TrailerDictionary(
            Integer size,
            Integer? prev,
            IndirectObjectReference root,
            Dictionary? encrypt,
            IndirectObjectReference? info,
            ArrayObject? id
            )
        {
            Size = size;
            
            if (prev is not null)
            {
                Prev = prev;
            }
            
            Root = root;

            if (encrypt is not null)
            {
                Encrypt = encrypt;
            }

            if (info is not null)
            {
                Info = info;
            }

            if (id is not null)
            {
                ID = id;
            }
        }

        private TrailerDictionary(Dictionary trailerDictionary) : base(trailerDictionary) { }

        /// <summary>
        /// The total number of entries in the PDF file's cross-reference table, as defined by the combination of the original section and all update sections.
        /// Equivalently, this value shall be 1 greater than the highest object number defined in the PDF file.
        /// </summary>
        public Integer Size { get => Get<Integer>(DictionaryKeys.Size)!; private set => this[DictionaryKeys.Size] = value; }

        /// <summary>
        /// Optional, present only if the file has more than one cross-reference section;
        /// The byte offset from the beginning of the PDF file to the beginning of the previous cross-reference section.
        /// </summary>
        public Integer? Prev { get => Get<Integer>(DictionaryKeys.Prev); private set => this[DictionaryKeys.Prev] = value!; }

        /// <summary>
        /// The catalog dictionary for the PDF file.
        /// </summary>
        public IndirectObjectReference Root { get => Get<IndirectObjectReference>(DictionaryKeys.Root)!; private set => this[DictionaryKeys.Root] = value; }

        /// <summary>
        /// Required if document is encrypted; Added in PDF 1.1.
        /// </summary>
        public Dictionary? Encrypt { get => Get<Dictionary>(DictionaryKeys.Encrypt); private set => this[DictionaryKeys.Encrypt] = value!; }

        /// <summary>
        /// Optional. Deprecated in PDF 2.0. The PDF file's information dictionary.<para></para>
        /// N.B. The ModDate key within the Info dictionary is required if Page-Piece dictionaries are used. 
        /// </summary>
        public IndirectObjectReference? Info { get => Get<IndirectObjectReference>(DictionaryKeys.Info); private set => this[DictionaryKeys.Info] = value!; }

        /// <summary>
        /// Required in PDF 2.0 and later, or if an Encrypt entry is present; optional otherwise; Added in PDF 1.1.
        /// </summary>
        public ArrayObject? ID { get => Get<ArrayObject>(DictionaryKeys.ID); private set => this[DictionaryKeys.ID] = value!; }

        /// <summary>
        /// Create a page from an existing page dictionary.
        /// </summary>
        /// <param name="trailerDictionary">An existing dictionary from which to create the <see cref="TrailerDictionary"/>.</param>
        /// <returns>A <see cref="TrailerDictionary"/> instance.</returns>
        internal static TrailerDictionary FromDictionary(Dictionary trailerDictionary)
        {
            if (trailerDictionary is null)
            {
                throw new ArgumentNullException(nameof(trailerDictionary));
            }

            if (trailerDictionary.Get<Integer>(DictionaryKeys.Size) is null)
            {
                throw new ArgumentException($"Missing required {DictionaryKeys.Size} entry in {trailerDictionary}", nameof(trailerDictionary));
            }

            if (trailerDictionary.Get<IndirectObjectReference>(DictionaryKeys.Root) is null)
            {
                throw new ArgumentException($"Missing required {DictionaryKeys.Root} entry in {trailerDictionary}", nameof(trailerDictionary));
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
            if (size is null) throw new ArgumentNullException(nameof(size));
            if (root is null) throw new ArgumentNullException(nameof(root));

            return new(size, prev, root, encrypt, info, id);
        }
    }
}
