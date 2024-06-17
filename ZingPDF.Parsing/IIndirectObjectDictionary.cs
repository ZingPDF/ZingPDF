using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF.Parsing
{
    public interface IIndirectObjectDictionary
    {
        int Count { get; }

        Task<IndirectObject?> GetAsync(IndirectObjectReference key);
        Task<T?> GetAsync<T>(IndirectObjectReference key);
    }
}