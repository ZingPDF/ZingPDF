using ZingPDF.IncrementalUpdates;

namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

public class RequiredMultiProperty<T1, T2>(Name key, Dictionary dictionary, IPdfEditor pdfEditor)
    : BaseProperty(key, dictionary, pdfEditor)
    where T1 : class, IPdfObject
    where T2 : class, IPdfObject
{
    public async Task<Either<T1, T2>> GetAsync()
    {
        var value = await ResolveAsync() ?? throw new InvalidPdfException($"Missing value for required property: {key}");

        return value switch
        {
            T1 t1 => new Either<T1, T2>(t1),
            T2 t2 => new Either<T1, T2>(t2),
            _ => throw new InvalidOperationException($"Requested Either<{typeof(T1)},{typeof(T2)}> instance cannot contain type: {value.GetType()}")
        };
    }
}
