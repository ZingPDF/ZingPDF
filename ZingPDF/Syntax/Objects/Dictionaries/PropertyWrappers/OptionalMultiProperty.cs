namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

public class OptionalMultiProperty<T1, T2> : BaseProperty
    where T1 : class, IPdfObject
    where T2 : class, IPdfObject
{
    public OptionalMultiProperty(string key, Dictionary dictionary, IPdf pdf)
        : base(key, dictionary, pdf)
    {
    }

    public async Task<Either<T1, T2>> GetAsync()
    {
        var value = await ResolveAsync();

        if (value is null)
        {
            return new Either<T1, T2>((T1)null!, ObjectOrigin.None);
        }

        return value switch
        {
            T1 t1 => new Either<T1, T2>(t1, t1.Origin),
            T2 t2 => new Either<T1, T2>(t2, t2.Origin),
            _ => throw new InvalidOperationException($"Requested Either<{typeof(T1)},{typeof(T2)}> instance cannot contain type: {value.GetType()}")
        };
    }
}
