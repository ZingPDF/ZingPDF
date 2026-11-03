namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

public class RequiredMultiProperty<T1, T2>(string key, Dictionary dictionary, IPdf pdf)
    : BaseProperty(key, dictionary, pdf)
    where T1 : class, IPdfObject
    where T2 : class, IPdfObject
{
    public async Task<Either<T1, T2>> GetAsync()
    {
        var value = await ResolveAsync() ?? throw new InvalidPdfException($"Missing value for required property: {Key}");

        if (TryConvertValue<T1>(value) is { } first) return new Either<T1, T2>(first, first.Context);
        if (TryConvertValue<T2>(value) is { } second) return new Either<T1, T2>(second, second.Context);
        throw new InvalidOperationException($"Requested Either<{typeof(T1)},{typeof(T2)}> instance cannot contain type: {value.GetType()}");
    }
}
