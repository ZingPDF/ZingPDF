using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.Objects.Dictionaries;

/// <summary>
/// Wrapper for a property which must be accessed asynchronously.
/// </summary>
/// <remarks>
/// Most dictionary values can be either a direct object, or an indirect object reference.
/// When they are a reference, the property value is represented as an indirect object elsewhere in the PDF.
/// This class exposes a <see cref="GetAsync"/> method which resolves the value in either case.
/// </remarks>
public class AsyncProperty<T>(IPdfObject value, IPdfEditor pdfEditor) where T : class, IPdfObject
{
    public IPdfObject Value => value;

    /// <summary>
    /// Retrieve the property value.
    /// </summary>
    /// <remarks>
    /// Returns the property value, whether it is a direct object or indirect object reference.
    /// </remarks>
    /// <returns>An instance of <see cref="T"/></returns>
    /// <exception cref="InvalidPdfException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<T> GetAsync()
    {
        if (value is T typed)
        {
            return typed;
        }
        else if (value is IndirectObjectReference ior)
        {
            return await pdfEditor.GetAsync<T>(ior)
                ?? throw new InvalidPdfException($"Unable to resolve indirect object reference: {ior}");
        }

        throw new InvalidOperationException("Internal error - invalid property type");
    }

    /// <summary>
    /// Gets the wrapper indirect object for the property.
    /// </summary>
    /// <returns>An <see cref="IndirectObject"/> containing the property value</returns>
    /// <exception cref="InvalidOperationException">Thrown if called on a property which is not an <see cref="IndirectObjectReference"/></exception>
    public async Task<IndirectObject> GetIndirectObjectAsync()
    {
        if (value is IndirectObjectReference ior)
        {
            return await pdfEditor.GetAsync(ior);
        }
        
        throw new InvalidOperationException($"Internal error - Attempt to call GetIndirectObjectAsync. Value is {value?.GetType().Name}");
    }
}
