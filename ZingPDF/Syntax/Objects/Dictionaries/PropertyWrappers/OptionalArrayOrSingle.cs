using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

public class OptionalArrayOrSingle<T>(string key, Dictionary dictionary, IPdf pdf)
    : BaseProperty(key, dictionary, pdf) where T : class, IPdfObject
{
    public async Task<ArrayObject?> GetAsync()
    {
        var rawValue = await ResolveAsync();

        if (rawValue is null)
        {
            return null;
        }

        if (rawValue is T typed)
        {
            return new ArrayObject([typed], typed.Context);
        }
        else if (rawValue is ArrayObject ary)
        {
            for (var i = 0; i < ary.Count(); i++)
            {
                var value = ary[i] as IndirectObjectReference;
                if (value is not null)
                {
                    ary[i] = (await PdfObjects.GetAsync(value)).Object;
                }
            }

            return ary;
        }

        throw new InvalidOperationException("Internal error - invalid property type");
    }
}
