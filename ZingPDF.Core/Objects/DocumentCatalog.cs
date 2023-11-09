using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Objects
{
    internal class DocumentCatalog : Dictionary
    {
        private static class DictionaryKeys
        {
            public const string Type = "Type";
            public const string Pages = "Pages";
        }

        private DocumentCatalog(Dictionary documentCatalogDictionary) : base(documentCatalogDictionary) { }

        private DocumentCatalog(IndirectObjectReference pageTreeNode)
            : base(new Dictionary<Name, PdfObject>()
            {
                { DictionaryKeys.Type, new Name("Catalog") },
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
