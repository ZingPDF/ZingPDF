using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

/// <summary>
/// Provides low-level access to the PDF object graph for advanced inspection and editing scenarios.
/// </summary>
public interface IPdfObjectCollection : IAsyncEnumerable<IndirectObject>
{
    /// <summary>
    /// Gets the document catalog of the PDF.
    /// </summary>
    Task<DocumentCatalogDictionary> GetDocumentCatalogAsync();

    /// <summary>
    /// Gets the document's latest trailer dictionary.
    /// </summary>
    Task<ITrailerDictionary> GetLatestTrailerDictionaryAsync();

    /// <summary>
    /// Gets the document page tree helper.
    /// </summary>
    PageTree PageTree { get; }

    /// <summary>
    /// Gets an object from the PDF by its object reference.
    /// </summary>
    Task<IndirectObject> GetAsync(IndirectObjectReference key);

    /// <summary>
    /// Gets an object from the PDF by its object reference. This method unwraps the object from its <see cref="IndirectObject"/> wrapper.
    /// </summary>
    Task<T> GetAsync<T>(IndirectObjectReference key) where T : class?, IPdfObject?;

    /// <summary>
    /// Adds a new indirect object to the PDF.
    /// </summary>
    /// <returns>
    /// The newly assigned indirect object wrapper.
    /// </returns>
    Task<IndirectObject> AddAsync(IPdfObject pdfObject);

    /// <summary>
    /// Convenience method to add multiple objects at once.
    /// </summary>
    Task AddRangeAsync(IEnumerable<IPdfObject> pdfObjects);

    /// <summary>
    /// Replace an existing object with a new version.
    /// </summary>
    /// <remarks>
    /// The old object is not removed. A new version is added to the PDF with the same object reference.
    /// </remarks>
    void Update(IndirectObject indirectObject);

    /// <summary>
    /// Mark an object for deletion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The object will not be removed from the PDF, but is marked as deleted.
    /// The object will no longer be returned by the <see cref="GetAsync(IndirectObjectReference)"/> or <see cref="GetAsync{T}(IndirectObjectReference)"/> methods.
    /// </para>
    /// <para>
    /// When the <see cref="GenerateUpdateDeltaAsync"/> method is called, the object itself remains in the file, but its cross reference entry is marked as 'free'.
    /// </para>
    /// </remarks>
    void Delete(IndirectObjectId indirectObjectId);

    /// <summary>
    /// Creates a new incremental update from added, updated, and removed objects.
    /// </summary>
    /// <remarks>
    /// Returns null if there have been no updates to the PDF.
    /// </remarks>
    Task<IncrementalUpdate?> GenerateUpdateDeltaAsync(bool includeAllObjects = false);
}
