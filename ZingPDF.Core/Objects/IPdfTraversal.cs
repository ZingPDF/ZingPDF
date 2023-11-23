using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Objects
{
    internal interface IPdfTraversal
    {
        /// <summary>
        /// Get the root page tree node.
        /// </summary>
        Task<IndirectObject> GetRootPageTreeNodeAsync(TrailerDictionary trailerDictionary, bool linearizedPdf);

        /// <summary>
        /// Get all pages.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Page>> GetPagesAsync(TrailerDictionary trailerDictionary, bool linearizedPdf);

        /// <summary>
        /// Get the most recent trailer.
        /// </summary>
        Task<Trailer> GetLatestTrailerAsync(bool linearizedPdf);

        /// <summary>
        /// Get all cross references, made up from all tables.
        /// </summary>
        Task<IEnumerable<CrossReferenceEntry>> GetAggregateCrossReferencesAsync(bool linearizedPdf);
    }
}
