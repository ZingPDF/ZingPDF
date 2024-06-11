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

        public IndirectObjectReference Pages { get => Get<IndirectObjectReference>(DictionaryKeys.Pages)!; }

        public static DocumentCatalogDictionary FromDictionary(Dictionary documentCatalogDictionary)
        {
            if (documentCatalogDictionary is null) throw new ArgumentNullException(nameof(documentCatalogDictionary));

            return new(documentCatalogDictionary);
        }

        public static DocumentCatalogDictionary CreateNew(IndirectObjectReference pageTreeNode)
        {
            if (pageTreeNode is null) throw new ArgumentNullException(nameof(pageTreeNode));

            return new(pageTreeNode);
        }
    }
}
