using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.Objects.Dictionaries;

/// <summary>
/// Wrapper for a property which must be accessed asynchronously.
/// </summary>
/// <remarks>
/// Some dictionary values can be either a direct object, or an indirect object reference.
/// When they are a reference, the property value is represented as an indirect object elsewhere in the PDF.
/// This class exposes a <see cref="GetAsync(IIndirectObjectDictionary)"/> method which resolves the value.
/// </remarks>
public class AsyncProperty<T>(IPdfObject? value) where T : class, IPdfObject
{
    public async Task<T> GetAsync(IIndirectObjectDictionary indirectObjectDictionary)
    {
        if (value is T typed)
        {
            return typed;
        }
        else if (value is IndirectObjectReference ior)
        {
            return await indirectObjectDictionary.GetAsync<T>(ior)
                ?? throw new InvalidPdfException($"Unable to resolve indirect object reference: {ior}");
        }

        throw new InvalidOperationException("Internal error - invalid property type");
    }
}
