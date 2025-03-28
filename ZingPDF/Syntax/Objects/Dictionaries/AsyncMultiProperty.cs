using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.Objects.Dictionaries;

public class AsyncMultiProperty<T1, T2>(IPdfObject value, IPdfEditor pdfEditor)
    where T1 : class, IPdfObject
    where T2 : class, IPdfObject
{
    public async Task<Either<T1, T2>> GetAsync()
    {
        if (value is IndirectObjectReference ior)
        {
            var indirectObject = await pdfEditor.GetAsync(ior)
                ?? throw new InvalidPdfException($"Unable to resolve indirect object reference: {ior}");

            value = indirectObject.Object;
        }

        return value switch
        {
            T1 t1 => new Either<T1, T2>(t1),
            T2 t2 => new Either<T1, T2>(t2),
            _ => throw new InvalidOperationException($"Requested Either<{typeof(T1)},{typeof(T2)}> instance cannot contain type: {value.GetType()}")
        };
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
