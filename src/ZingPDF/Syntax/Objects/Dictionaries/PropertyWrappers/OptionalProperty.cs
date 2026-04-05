namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

/// <summary>
/// Wrapper for an optional value.
/// </summary>
/// <typeparam name="T">The PDF object type exposed by the property.</typeparam>
/// <remarks>
/// Most dictionary values can be either a direct object, or an indirect object reference.
/// When they are a reference, the property value is represented as an indirect object elsewhere in the PDF.
/// This class exposes a <see cref="GetAsync"/> method which resolves the value in either case.
/// </remarks>
public class OptionalProperty<T>(string key, Dictionary dictionary, IPdf pdf)
    : BaseProperty(key, dictionary, pdf) where T : class, IPdfObject
{
    /// <summary>
    /// Retrieve the property value.
    /// </summary>
    /// <remarks>
    /// Returns the property value, whether it is a direct object or indirect object reference.
    /// </remarks>
    /// <returns>An instance of <typeparamref name="T"/> when present; otherwise <see langword="null"/>.</returns>
    /// <exception cref="InvalidPdfException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<T?> GetAsync()
    {
        var value = await ResolveAsync();

        if (value is null)
        {
            return null;
        }

        return value as T
            ?? throw new InvalidOperationException($"Requested type {typeof(T)} cannot contain type: {value.GetType()}");
    }
}
