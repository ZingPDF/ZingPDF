using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF
{
    public interface IIndirectObjectDictionary
    {
        /// <summary>
        /// The total number of objects in the PDF.
        /// </summary>
        /// <remarks>
        /// This is a count of active objects in the PDF. It does not include deleted objects, 
        /// and will include an object once even if it has been updated and exists in multiple places.
        /// </remarks>
        int Count { get; }

        /// <summary>
        /// Indicates whether the dictionary contains the specified key.
        /// </summary>
        bool ContainsKey(IndirectObjectReference key);

        /// <summary>
        /// Get an object by its reference.
        /// </summary>
        Task<IndirectObject> GetAsync(IndirectObjectReference key);

        /// <summary>
        /// Get an object by its reference. This method unwraps the object from its <see cref="IndirectObject"/>.
        /// </summary>
        Task<T?> GetAsync<T>(IndirectObjectReference key) where T : class, IPdfObject;
    }
}