using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core
{
    internal interface IPdfNavigator
    {
        bool UsingXrefStreams { get; }
        bool UsingXrefTables { get; }

        Task<IndirectObject> DereferenceIndirectObjectAsync(IndirectObjectReference reference);
        Task<Dictionary<int, CrossReferenceEntry>> GetAggregateCrossReferencesAsync();
        Task<LinearizationDictionary?> GetLinearizationDictionaryAsync();
        Task<IEnumerable<IndirectObject>> GetPagesAsync();
        Task<IndirectObject> GetRootPageTreeNodeAsync();
        Task<Trailer?> GetRootTrailerAsync();
        Task<ITrailerDictionary> GetRootTrailerDictionaryAsync();
    }
}