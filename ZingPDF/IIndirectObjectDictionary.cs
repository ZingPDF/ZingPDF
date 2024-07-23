using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF
{
    public interface IIndirectObjectDictionary
    {
        int Count { get; }

        Task<IndirectObject?> GetAsync(IndirectObjectReference key);
        Task<T?> GetAsync<T>(IndirectObjectReference key);
        List<IndirectObjectId> GetFreeIds();
    }
}