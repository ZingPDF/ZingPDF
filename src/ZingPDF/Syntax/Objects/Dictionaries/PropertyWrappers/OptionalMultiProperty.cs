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
            return new Either<T1, T2>((T1)null!, ObjectContext.None);
        }

        if (TryConvertValue<T1>(value) is { } first) return new Either<T1, T2>(first, first.Context);
        if (TryConvertValue<T2>(value) is { } second) return new Either<T1, T2>(second, second.Context);
        throw new InvalidOperationException($"Requested Either<{typeof(T1)},{typeof(T2)}> instance cannot contain type: {value.GetType()}");
    }
}
