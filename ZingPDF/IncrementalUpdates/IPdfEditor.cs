using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.IncrementalUpdates;

public interface IPdfEditor : IAsyncEnumerable<IndirectObject>
{
    /// <summary>
    /// Gets an object from the PDF by its object reference.
    /// </summary>
    Task<IndirectObject> GetAsync(IndirectObjectReference key);

    /// <summary>
    /// Gets an object from the PDF by its object reference. This method unwraps the object from its <see cref="IndirectObject"/> wrapper.
    /// </summary>
    Task<T> GetAsync<T>(IndirectObjectReference key) where T : class?, IPdfObject?;

    /// <summary>
    /// Add a new object to the PDF.
    /// </summary>
    /// <returns>
    /// An <see cref="IndirectObject"/> which wraps the provided object.
    /// </returns>
    IndirectObject Add(IPdfObject pdfObject);

    /// <summary>
    /// Convenience method to add multiple objects at once.
    /// </summary>
    void AddRange(IEnumerable<IPdfObject> pdfObjects);

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
    Task<IncrementalUpdate?> GenerateUpdateDeltaAsync();
}
