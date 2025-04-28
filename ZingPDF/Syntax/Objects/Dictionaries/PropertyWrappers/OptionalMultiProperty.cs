using ZingPDF.IncrementalUpdates;

namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

public class OptionalMultiProperty<T1, T2> : BaseProperty
    where T1 : class, IPdfObject
    where T2 : class, IPdfObject
{
    public OptionalMultiProperty(Name key, Dictionary dictionary, IPdfEditor pdfEditor)
        : base(key, dictionary, pdfEditor)
    {
    }

    public async Task<Either<T1, T2>> GetAsync()
    {
        var value = await ResolveAsync();

        if (value is null)
        {
            return new Either<T1, T2>((T1)null!);
        }

        return value switch
        {
            T1 t1 => new Either<T1, T2>(t1),
            T2 t2 => new Either<T1, T2>(t2),
            _ => throw new InvalidOperationException($"Requested Either<{typeof(T1)},{typeof(T2)}> instance cannot contain type: {value.GetType()}")
        };
    }
}
