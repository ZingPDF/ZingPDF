using ZingPDF.IncrementalUpdates;

namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

/// <summary>
/// Wrapper for a value which must be accessed asynchronously.
/// </summary>
/// <remarks>
/// Most dictionary values can be either a direct object, or an indirect object reference.
/// When they are a reference, the property value is represented as an indirect object elsewhere in the PDF.
/// This class exposes a <see cref="GetAsync"/> method which resolves the value in either case.
/// </remarks>
public class DictionaryProperty<T>(Name key, Dictionary dictionary, IPdfEditor pdfEditor)
    : BaseDictionaryProperty(key, dictionary, pdfEditor) where T : class?, IPdfObject?
{
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
        var value = await ResolveAsync();

        if (value is null)
        {
            // The compiler should be able to infer that this is ok from the `class?` constraint, but it doesn't
            return null!;
        }

        return value as T
            ?? throw new InvalidOperationException($"Requested type {typeof(T)} cannot contain type: {value.GetType()}");
    }
}
