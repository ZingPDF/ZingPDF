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
        PageTreeNode GetPageTreeNode(IndirectObject pagesCatalogIndirectObject);

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
            var documentCatalogReference = trailerDictionary.Get<IndirectObjectReference>("Root")!;

            return DocumentCatalog.FromDictionary(indirectObjectManager.GetSingle<Dictionary>(documentCatalogReference.Id));
        }

        public PageTreeNode GetRootPageTreeNode(Dictionary trailerDictionary, IndirectObjectManager indirectObjectManager)
        {
            var documentCatalog = GetDocumentCatalog(trailerDictionary, indirectObjectManager);

            return GetPageTreeNode(indirectObjectManager[documentCatalog.Pages.Id]);
        }

        public PageTreeNode GetPageTreeNode(IndirectObject pagesCatalogIndirectObject)
            => PageTreeNode.FromDictionary((pagesCatalogIndirectObject.Children.First() as Dictionary)!);

        public Dictionary GetLatestTrailerDictionary(IEnumerable<PdfIncrement> increments)
            => increments.Last().Trailer.Dictionary;
    }
}
