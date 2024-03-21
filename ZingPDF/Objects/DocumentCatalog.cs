using ZingPDF.Objects;
using ZingPDF.Objects.Primitives;
using ZingPDF.Objects.Primitives.IndirectObjects;

namespace ZingPDF.Objects
{
    internal class DocumentCatalog : Dictionary
    {
        private static class DictionaryKeys
        {
            public const string Catalog = "Catalog";
            public const string Pages = "Pages";
        }

        private DocumentCatalog(Dictionary documentCatalogDictionary) : base(documentCatalogDictionary) { }

        private DocumentCatalog(IndirectObjectReference pageTreeNode)
            : base(new Dictionary<Name, IPdfObject>()
            {
                { Constants.DictionaryKeys.Type, new Name(DictionaryKeys.Catalog) },
                { DictionaryKeys.Pages, pageTreeNode },
            })
        { }

        public IndirectObjectReference Pages { get => Get<IndirectObjectReference>(DictionaryKeys.Pages)!; }

        public static DocumentCatalog FromDictionary(Dictionary documentCatalogDictionary)
        {
            if (documentCatalogDictionary is null) throw new ArgumentNullException(nameof(documentCatalogDictionary));

            return new(documentCatalogDictionary);
        }

        public static DocumentCatalog CreateNew(IndirectObjectReference pageTreeNode)
        {
            if (pageTreeNode is null) throw new ArgumentNullException(nameof(pageTreeNode));

            return new(pageTreeNode);
        }
    }
}
