using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

public abstract class BaseDictionaryProperty
{
    private readonly Dictionary _dictionary;
    private readonly Name _key;

    protected readonly IPdfEditor _pdfEditor;

    public BaseDictionaryProperty(Name key, Dictionary dictionary, IPdfEditor pdfEditor)
    {
        _key = key;
        _pdfEditor = pdfEditor ?? throw new ArgumentNullException(nameof(pdfEditor));
        _dictionary = dictionary;
    }

    public Name Key => _key;

    /// <summary>
    /// Gets the related indirect object for the property. The property value must be an indirect object reference.
    /// </summary>
    /// <returns>An <see cref="IndirectObject"/> containing the property value</returns>
    /// <exception cref="InvalidOperationException">Thrown if called on a property which is not an <see cref="IndirectObjectReference"/></exception>
    public async Task<IndirectObject> GetIndirectObjectAsync()
    {
        var value = await GetRawValueAsync();

        if (value is IndirectObjectReference ior)
        {
            return await _pdfEditor.GetAsync(ior);
        }

        throw new InvalidOperationException($"Internal error - Attempt to call GetIndirectObjectAsync. Value is {value?.GetType().Name}");
    }

    /// <summary>
    /// Gets the raw value of the property. If the value is an indirect reference, the indirect object 
    /// is not resolved and the reference itself is returned.
    /// </summary>
    /// <remarks>
    /// Returns the raw value of the property, whether it is a direct object or indirect object reference.
    /// If the value is marked as inheritable, this method will attempt to retrieve the value from the parent dictionary.
    /// </remarks>
    public async Task<IPdfObject?> GetRawValueAsync()
    {
        var value = _dictionary.GetAs<IPdfObject>(_key);
        if (value != null)
        {
            return value;
        }

        var parentRef = _dictionary.GetAs<IndirectObjectReference?>(Constants.DictionaryKeys.Parent);
        if (parentRef == null)
        {
            return null;
        }

        if (!GeneratedInheritableKeys.InheritableKeys.Map.TryGetValue(_dictionary.GetType(), out var inheritableProperties))
        {
            return null;
        }

        if (!inheritableProperties.Contains(_key))
        {
            return null;
        }

        var parentDictionary = await _pdfEditor.GetAsync<Dictionary>(parentRef) 
            ?? throw new InvalidPdfException($"Invalid parent reference: {parentRef}");

        return await parentDictionary
            .Get<IPdfObject>(_key)
            .GetRawValueAsync(); // Recurse
    }

    public async Task<IPdfObject?> ResolveAsync()
    {
        var rawValue = await GetRawValueAsync();

        if (rawValue is null)
        {
            return null;
        }

        if (rawValue is IndirectObjectReference ior)
        {
            var dereferenced = await _pdfEditor.GetAsync(ior)
                ?? throw new InvalidPdfException($"Unable to resolve indirect object reference: {ior}");

            return dereferenced.Object;
        }

        return rawValue;
    }
}
