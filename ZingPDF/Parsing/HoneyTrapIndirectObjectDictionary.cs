using ZingPDF;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Parsing;

internal class HoneyTrapIndirectObjectDictionary : IIndirectObjectDictionary
{
    private static readonly IIndirectObjectDictionary _instance = new HoneyTrapIndirectObjectDictionary();

    private const string _error = "If you're seeing this, ZingPDF is broken.";

    public int Count => throw new InvalidOperationException(_error);
    public Task<IndirectObject?> GetAsync(IndirectObjectReference key) => throw new InvalidOperationException(_error);
    public List<IndirectObjectId> GetFreeIds() => throw new InvalidOperationException(_error);
    Task<T?> IIndirectObjectDictionary.GetAsync<T>(IndirectObjectReference key) where T : class => throw new InvalidOperationException(_error);
    public IndirectObject Add(IPdfObject pdfObject) => throw new NotImplementedException(_error);
    public void AddRange(IEnumerable<IPdfObject> pdfObjects) => throw new NotImplementedException(_error);
    public void Update(IndirectObject indirectObject) => throw new NotImplementedException(_error);
    public void Delete(IndirectObjectId indirectObjectId) => throw new NotImplementedException(_error);
    public Task IndexObjectsAsync() => throw new NotImplementedException(_error);
    public Task<IncrementalUpdate?> GenerateUpdateDeltaAsync() => throw new NotImplementedException(_error);

    public static IIndirectObjectDictionary Instance => _instance;
}