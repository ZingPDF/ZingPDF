namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

public class RequiredArrayOrSingle<T>(string key, Dictionary dictionary, IPdf pdf)
    : BaseProperty(key, dictionary, pdf) where T : class, IPdfObject
{
    public async Task<ArrayObject> GetAsync()
    {
        var rawValue = await ResolveAsync() ?? throw new InvalidPdfException($"Missing value for required property: {Key}");

        if (rawValue is T typed)
        {
            return new ArrayObject([typed], typed.Context);
        }
        else if (rawValue is ArrayObject ary)
        {
            return ary;
        }

        throw new InvalidOperationException("Internal error - invalid property type");
    }
}
