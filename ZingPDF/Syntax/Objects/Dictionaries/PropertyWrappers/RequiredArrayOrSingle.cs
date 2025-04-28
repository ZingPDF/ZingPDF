using ZingPDF.IncrementalUpdates;

namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

public class RequiredArrayOrSingle<T>(Name key, Dictionary dictionary, IPdfEditor pdfEditor)
    : BaseProperty(key, dictionary, pdfEditor) where T : class, IPdfObject
{
    public async Task<ArrayObject> GetAsync()
    {
        var rawValue = await ResolveAsync() ?? throw new InvalidPdfException($"Missing value for required property: {key}");

        if (rawValue is T typed)
        {
            return [typed];
        }
        else if (rawValue is ArrayObject ary)
        {
            return ary;
        }

        throw new InvalidOperationException("Internal error - invalid property type");
    }
}
