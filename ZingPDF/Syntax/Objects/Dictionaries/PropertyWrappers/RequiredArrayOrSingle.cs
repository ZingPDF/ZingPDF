namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

public class RequiredArrayOrSingle<T>(string key, Dictionary dictionary, IPdfContext pdfContext)
    : BaseProperty(key, dictionary, pdfContext) where T : class, IPdfObject
{
    public async Task<ArrayObject> GetAsync()
    {
        var rawValue = await ResolveAsync() ?? throw new InvalidPdfException($"Missing value for required property: {Key}");

        if (rawValue is T typed)
        {
            return new ArrayObject([typed], typed.Origin);
        }
        else if (rawValue is ArrayObject ary)
        {
            return ary;
        }

        throw new InvalidOperationException("Internal error - invalid property type");
    }
}
