using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Objects
{
    internal interface IPdfTraversal
    {
        /// <summary>
        /// Get the linearization dictionary.
        /// </summary>
        Task<LinearizationDictionary?> GetLinearizationDictionaryAsync();

        /// <summary>
        /// Get the root trailer. For PDFs using cross reference streams, this will return null.
        /// </summary>
        Task<Trailer?> GetRootTrailerAsync();

        /// <summary>
        /// Get the root trailer dictionary. This may come from the file trailer if it exists, or a cross reference stream.<para></para>
        /// </summary>
        Task<ITrailerDictionary> GetRootTrailerDictionaryAsync();

        /// <summary>
        /// Get the root page tree node.
        /// </summary>
        Task<IndirectObject> GetRootPageTreeNodeAsync();

        /// <summary>
        /// Get all pages.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Page>> GetPagesAsync();

        /// <summary>
        /// Get all cross references, made up from all tables.
        /// </summary>
        Task<IEnumerable<CrossReferenceEntry>> GetAggregateCrossReferencesAsync();
    }
}
