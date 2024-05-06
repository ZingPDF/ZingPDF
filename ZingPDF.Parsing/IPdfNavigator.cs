using ZingPDF.Objects;
using ZingPDF.Objects.ObjectGroups.CrossReferences;
using ZingPDF.Objects.ObjectGroups.Trailer;
using ZingPDF.Objects.Primitives.IndirectObjects;

namespace ZingPDF.Parsing
{
    internal interface IPdfNavigator
    {
        bool UsingXrefStreams { get; }
        bool UsingXrefTables { get; }

        Task<int> GetStartXrefAsync();

        /// <summary>
        /// Returns the latest Indirect Object matching the given reference.
        /// </summary>
        Task<IndirectObject> DereferenceIndirectObjectAsync(IndirectObjectReference reference);

        /// <summary>
        /// When you know the Indirect Object contains a single object of a specific type, 
        /// this method provides strongly typed access to it.
        /// </summary>
        Task<T> DereferenceIndirectObjectAsync<T>(IndirectObjectReference reference) where T : PdfObject;

        Task<Dictionary<int, CrossReferenceEntry>> GetAggregateCrossReferencesAsync();
        Task<LinearizationDictionary?> GetLinearizationDictionaryAsync();
        Task<IEnumerable<IndirectObject>> GetPagesAsync();
        Task<IndirectObject> GetRootPageTreeNodeAsync();
        Task<Trailer?> GetRootTrailerAsync();
        Task<ITrailerDictionary> GetRootTrailerDictionaryAsync();
    }
}