using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

public class OptionalArrayOrSingle<T>(Name key, Dictionary dictionary, IPdfEditor pdfEditor)
    : BaseDictionaryProperty(key, dictionary, pdfEditor) where T : class?, IPdfObject?
{
    public async Task<ArrayObject?> GetAsync()
    {
        var rawValue = await ResolveAsync();

        if (rawValue is null)
        {
            // The compiler should be able to infer that this is ok from the `class?` constraint, but it doesn't
            return null!;
        }

        if (rawValue is T typed)
        {
            return [typed];
        }
        else if (rawValue is ArrayObject ary)
        {
            for (var i = 0; i < ary.Count(); i++)
            {
                var value = ary[i] as IndirectObjectReference;
                if (value is not null)
                {
                    ary[i] = (await _pdfEditor.GetAsync(value)).Object;
                }
            }

            return ary;
        }

        throw new InvalidOperationException("Internal error - invalid property type");
    }
}
