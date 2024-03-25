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

        Task<IndirectObject> DereferenceIndirectObjectAsync(IndirectObjectReference reference);
        Task<Dictionary<int, CrossReferenceEntry>> GetAggregateCrossReferencesAsync();
        Task<LinearizationDictionary?> GetLinearizationDictionaryAsync();
        Task<IEnumerable<IndirectObject>> GetPagesAsync();
        Task<IndirectObject> GetRootPageTreeNodeAsync();
        Task<Trailer?> GetRootTrailerAsync();
        Task<ITrailerDictionary> GetRootTrailerDictionaryAsync();
    }
}