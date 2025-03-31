using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.IncrementalUpdates;

internal class EmptyPdfEditor : IPdfEditor
{
    private static readonly InvalidOperationException _internalError = new("Internal error: {D5A214B9-5FA4-454E-88F1-EE6C62D58429}");

    private static readonly Lazy<EmptyPdfEditor> _instance = new(() => new EmptyPdfEditor());
    public static EmptyPdfEditor Instance => _instance.Value;

    public IndirectObject Add(IPdfObject pdfObject) => throw _internalError;
    public void AddRange(IEnumerable<IPdfObject> pdfObjects) => throw _internalError;
    public void Delete(IndirectObjectId indirectObjectId) => throw _internalError;
    public Task<IncrementalUpdate?> GenerateUpdateDeltaAsync() => throw _internalError;
    public Task<IndirectObject> GetAsync(IndirectObjectReference key) => throw _internalError;
    public IAsyncEnumerator<IndirectObject> GetAsyncEnumerator(CancellationToken cancellationToken = default) => throw _internalError;
    public void Update(IndirectObject indirectObject) => throw _internalError;
    public Task<T> GetAsync<T>(IndirectObjectReference key) where T : class?, IPdfObject? => throw _internalError;
}
