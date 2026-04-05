namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

public class RequiredMultiProperty<T1, T2>(string key, Dictionary dictionary, IPdf pdf)
    : BaseProperty(key, dictionary, pdf)
    where T1 : class, IPdfObject
    where T2 : class, IPdfObject
{
    public async Task<Either<T1, T2>> GetAsync()
    {
        var value = await ResolveAsync() ?? throw new InvalidPdfException($"Missing value for required property: {Key}");

        return value switch
        {
            T1 t1 => new Either<T1, T2>(t1, t1.Context),
            T2 t2 => new Either<T1, T2>(t2, t2.Context),
            _ => throw new InvalidOperationException($"Requested Either<{typeof(T1)},{typeof(T2)}> instance cannot contain type: {value.GetType()}")
        };
    }
}
