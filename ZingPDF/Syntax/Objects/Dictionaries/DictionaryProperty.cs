using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.Objects.Dictionaries;

/// <summary>
/// Wrapper for a value which must be accessed asynchronously.
/// </summary>
/// <remarks>
/// Most dictionary values can be either a direct object, or an indirect object reference.
/// When they are a reference, the property value is represented as an indirect object elsewhere in the PDF.
/// This class exposes a <see cref="GetAsync"/> method which resolves the value in either case.
/// </remarks>
public class DictionaryProperty<T> : BaseDictionaryProperty where T : class?, IPdfObject?
{
    public DictionaryProperty(Name key, Dictionary dictionary, IPdfEditor pdfEditor)
        : base(key, dictionary, pdfEditor)
    {
    }

    /// <summary>
    /// Retrieve the property value.
    /// </summary>
    /// <remarks>
    /// Returns the property value, whether it is a direct object or indirect object reference.
    /// </remarks>
    /// <returns>An instance of <see cref="T"/></returns>
    /// <exception cref="InvalidPdfException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<T> GetAsync()
    {
        var value = await GetRawValueAsync();

        if (value is null)
        {
            // The compiler should be able to infer that this is ok from the `class?` constraint, but it doesn't
            return null!;
        }

        if (value is T typed)
        {
            return typed;
        }
        else if (value is IndirectObjectReference ior)
        {
            return await _pdfEditor.GetAsync<T>(ior)
                ?? throw new InvalidPdfException($"Unable to resolve indirect object reference: {ior}");
        }

        throw new InvalidOperationException("Internal error - invalid property type");
    }
}
