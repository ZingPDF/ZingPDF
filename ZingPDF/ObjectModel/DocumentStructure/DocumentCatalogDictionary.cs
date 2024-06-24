using ZingPDF.ObjectModel.Objects;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF.ObjectModel.DocumentStructure
{
    /// <summary>
    /// ISO 32000-2:2020 7.7.2 - Document catalog dictionary
    /// </summary>
    public class DocumentCatalogDictionary : Dictionary
    {
        public static class DictionaryKeys
        {
            public const string Catalog = "Catalog";
            public const string Pages = "Pages";
        }

        private DocumentCatalogDictionary(Dictionary documentCatalogDictionary) : base(documentCatalogDictionary) { }

        private DocumentCatalogDictionary(IndirectObjectReference pageTreeNode)
            : base(new Dictionary<Name, IPdfObject>()
            {
                { Constants.DictionaryKeys.Type, new Name(DictionaryKeys.Catalog) },
                { DictionaryKeys.Pages, pageTreeNode },
            })
        { }

        /// <summary>
        /// (Required)<para></para>
        /// The page tree node that shall be the root of the document’s page tree (see 7.7.3, "Page tree").
        /// </summary>
        public IndirectObjectReference Pages => Get<IndirectObjectReference>(DictionaryKeys.Pages)!;

        public static DocumentCatalogDictionary FromDictionary(Dictionary documentCatalogDictionary)
        {
            return documentCatalogDictionary is null
                ? throw new ArgumentNullException(nameof(documentCatalogDictionary))
                : new(documentCatalogDictionary);
        }

        public static DocumentCatalogDictionary CreateNew(IndirectObjectReference pageTreeNode)
        {
            return pageTreeNode is null ? throw new ArgumentNullException(nameof(pageTreeNode)) : new(pageTreeNode);
        }
    }
}
