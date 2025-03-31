using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.Objects.Dictionaries;

public class DictionaryMultiProperty<T1, T2> : BaseDictionaryProperty
    where T1 : class?, IPdfObject?
    where T2 : class?, IPdfObject?
{
    public DictionaryMultiProperty(Name key, Dictionary dictionary, IPdfEditor pdfEditor)
        : base(key, dictionary, pdfEditor)
    {
    }

    public async Task<Either<T1, T2>> GetAsync()
    {
        var value = await GetRawValueAsync();

        if (value is null)
        {
            return new Either<T1, T2>((T1)null!);
        }

        if (value is IndirectObjectReference ior)
        {
            var indirectObject = await _pdfEditor.GetAsync(ior)
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
}
