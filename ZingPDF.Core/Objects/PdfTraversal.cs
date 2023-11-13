using ZingPdf.Core.Objects.Pages;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Objects
{
    internal interface IPdfTraversal
    {
        /// <summary>
        /// Get the root page tree node.
        /// </summary>
        PageTreeNode GetRootPageTreeNode(Dictionary trailerDictionary, IndirectObjectManager indirectObjectManager);

        /// <summary>
        /// Get a page tree node from its containing <see cref="IndirectObject"/> instance.
        /// </summary>
        PageTreeNode GetPageTreeNode(IndirectObject pageTreeNodeIndirectObject);

        /// <summary>
        /// Get all pages.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Page> GetPages(Dictionary trailerDictionary, IndirectObjectManager indirectObjectManager);

        /// <summary>
        /// Get the document catalog.
        /// </summary>
        DocumentCatalog GetDocumentCatalog(Dictionary trailerDictionary, IndirectObjectManager indirectObjectManager);

        /// <summary>
        /// Get the most recent trailer dictionary.
        /// </summary>
        Dictionary GetLatestTrailerDictionary(IEnumerable<PdfIncrement> increments);
    }

    internal class PdfTraversal : IPdfTraversal
    {
        public DocumentCatalog GetDocumentCatalog(Dictionary trailerDictionary, IndirectObjectManager indirectObjectManager)
        {
            if (trailerDictionary is null) throw new ArgumentNullException(nameof(trailerDictionary));
            if (indirectObjectManager is null) throw new ArgumentNullException(nameof(indirectObjectManager));

            var documentCatalogReference = trailerDictionary.Get<IndirectObjectReference>("Root")!;

            return DocumentCatalog.FromDictionary(indirectObjectManager.GetSingle<Dictionary>(documentCatalogReference.Id));
        }

        public PageTreeNode GetRootPageTreeNode(Dictionary trailerDictionary, IndirectObjectManager indirectObjectManager)
        {
            if (trailerDictionary is null) throw new ArgumentNullException(nameof(trailerDictionary));
            if (indirectObjectManager is null) throw new ArgumentNullException(nameof(indirectObjectManager));

            var documentCatalog = GetDocumentCatalog(trailerDictionary, indirectObjectManager);

            return GetPageTreeNode(indirectObjectManager[documentCatalog.Pages.Id]);
        }

        public IEnumerable<Page> GetPages(Dictionary trailerDictionary, IndirectObjectManager indirectObjectManager)
        {
            if (trailerDictionary is null) throw new ArgumentNullException(nameof(trailerDictionary));
            if (indirectObjectManager is null) throw new ArgumentNullException(nameof(indirectObjectManager));

            var rootPageTreeNode = GetRootPageTreeNode(trailerDictionary, indirectObjectManager);

            return GetSubPages(rootPageTreeNode, indirectObjectManager);
        }

        public PageTreeNode GetPageTreeNode(IndirectObject pageTreeNodeIndirectObject)
            => PageTreeNode.FromDictionary((pageTreeNodeIndirectObject.Children.First() as Dictionary)!);

        public Dictionary GetLatestTrailerDictionary(IEnumerable<PdfIncrement> increments)
            => increments.Last().Trailer.Dictionary;

        /// <summary>
        /// Recursively get all descendant subpages from the supplied <see cref="PageTreeNode"/>.
        /// </summary>
        private IEnumerable<Page> GetSubPages(PageTreeNode pageTreeNode, IndirectObjectManager indirectObjectManager)
        {
            List<Page> pages = new();

            var leafNodes = pageTreeNode.Kids
                .Cast<IndirectObjectReference>()
                .Where(k => indirectObjectManager[k.Id].Children.First() is Page)
                .Select(k => indirectObjectManager.GetSingle<Page>(k.Id));

            pages.AddRange(leafNodes);

            var childPageTreeNodes = pageTreeNode.Kids
                .Cast<IndirectObjectReference>()
                .Where(k => indirectObjectManager[k.Id].Children.First() is PageTreeNode)
                .Select(k => indirectObjectManager.GetSingle<PageTreeNode>(k.Id));

            pages.AddRange(childPageTreeNodes.SelectMany(node => GetSubPages(node, indirectObjectManager)));

            return pages;
        }
    }
}
