using ZingPdf.Core.Objects.ObjectGroups.Trailer;
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
        PageTreeNode GetRootPageTreeNode(TrailerDictionary trailerDictionary);

        /// <summary>
        /// Get a page tree node from its containing <see cref="IndirectObject"/> instance.
        /// </summary>
        PageTreeNode GetPageTreeNode(IndirectObject pageTreeNodeIndirectObject);

        /// <summary>
        /// Get all pages.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Page> GetPages(TrailerDictionary trailerDictionary);

        /// <summary>
        /// Get the document catalog.
        /// </summary>
        DocumentCatalog GetDocumentCatalog(TrailerDictionary trailerDictionary);

        /// <summary>
        /// Get the most recent trailer.
        /// </summary>
        Task<Trailer> GetLatestTrailerAsync();
    }
}
