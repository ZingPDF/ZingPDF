using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Pages;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Objects
{
    internal class ParsedPdfTraversal : IPdfTraversal
    {
        private readonly IndirectObjectManager _indirectObjectManager;

        public ParsedPdfTraversal(IndirectObjectManager indirectObjectManager)
        {
            _indirectObjectManager = indirectObjectManager ?? throw new ArgumentNullException(nameof(indirectObjectManager));
        }

        public DocumentCatalog GetDocumentCatalog(Dictionary trailerDictionary)
        {
            if (trailerDictionary is null) throw new ArgumentNullException(nameof(trailerDictionary));

            var documentCatalogReference = trailerDictionary.Get<IndirectObjectReference>("Root")!;

            return DocumentCatalog.FromDictionary(_indirectObjectManager.GetSingle<Dictionary>(documentCatalogReference.Id));
        }

        public PageTreeNode GetRootPageTreeNode(Dictionary trailerDictionary)
        {
            if (trailerDictionary is null) throw new ArgumentNullException(nameof(trailerDictionary));

            var documentCatalog = GetDocumentCatalog(trailerDictionary);

            return GetPageTreeNode(_indirectObjectManager[documentCatalog.Pages.Id]);
        }

        public IEnumerable<Page> GetPages(Dictionary trailerDictionary)
        {
            if (trailerDictionary is null) throw new ArgumentNullException(nameof(trailerDictionary));

            var rootPageTreeNode = GetRootPageTreeNode(trailerDictionary);

            return GetSubPages(rootPageTreeNode);
        }

        public PageTreeNode GetPageTreeNode(IndirectObject pageTreeNodeIndirectObject)
            => PageTreeNode.FromDictionary((pageTreeNodeIndirectObject.Children.First() as Dictionary)!);

        public Task<Trailer> GetLatestTrailerAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Recursively get all descendant subpages from the supplied <see cref="PageTreeNode"/>.
        /// </summary>
        private IEnumerable<Page> GetSubPages(PageTreeNode pageTreeNode)
        {
            List<Page> pages = new();

            var leafNodes = pageTreeNode.Kids
                .Cast<IndirectObjectReference>()
                .Where(k => _indirectObjectManager[k.Id].Children.First() is Page)
                .Select(k => _indirectObjectManager.GetSingle<Page>(k.Id));

            pages.AddRange(leafNodes);

            var childPageTreeNodes = pageTreeNode.Kids
                .Cast<IndirectObjectReference>()
                .Where(k => _indirectObjectManager[k.Id].Children.First() is PageTreeNode)
                .Select(k => _indirectObjectManager.GetSingle<PageTreeNode>(k.Id));

            pages.AddRange(childPageTreeNodes.SelectMany(node => GetSubPages(node)));

            return pages;
        }
    }
}
