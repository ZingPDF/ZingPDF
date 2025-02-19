using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.FileStructure.Trailer;

public class TrailerDictionary : Dictionary, ITrailerDictionary
{
    public static class DictionaryKeys
    {
        public const string Size = "Size";
        public const string Prev = "Prev";
        public const string Root = "Root";
        public const string Encrypt = "Encrypt";
        public const string Info = "Info";
        public const string ID = "ID";
    }

    private TrailerDictionary(Dictionary trailerDictionary) : base(trailerDictionary) { }

    public Integer Size => (Integer)this[DictionaryKeys.Size];
    public Integer? Prev => GetAs<Integer>(DictionaryKeys.Prev);
    public IndirectObjectReference? Root => GetAs<IndirectObjectReference>(DictionaryKeys.Root);
    public IndirectObjectReference? Encrypt => GetAs<IndirectObjectReference>(DictionaryKeys.Encrypt);
    public IndirectObjectReference? Info => GetAs<IndirectObjectReference>(DictionaryKeys.Info);
    public ArrayObject? ID => GetAs<ArrayObject>(DictionaryKeys.ID);

    /// <summary>
    /// Create a page from an existing page dictionary.
    /// </summary>
    /// <param name="trailerDictionary">An existing dictionary from which to create the <see cref="TrailerDictionary"/>.</param>
    /// <returns>A <see cref="TrailerDictionary"/> instance.</returns>
    internal static TrailerDictionary FromDictionary(Dictionary trailerDictionary)
    {
        ArgumentNullException.ThrowIfNull(trailerDictionary);

        if (trailerDictionary[DictionaryKeys.Size] == null)
        {
            throw new ArgumentException($"Missing required {DictionaryKeys.Size} entry in {trailerDictionary}", nameof(trailerDictionary));
        }

        return new TrailerDictionary(trailerDictionary);
    }

    /// <summary>
    /// Create a new <see cref="TrailerDictionary"/>.
    /// </summary>
    internal static TrailerDictionary CreateNew(
        Integer size,
        Integer? prev,
        IndirectObjectReference root,
        IPdfObject? encrypt,
        IndirectObjectReference? info,
        ArrayObject? id
        )
    {
        ArgumentNullException.ThrowIfNull(size);
        ArgumentNullException.ThrowIfNull(root);

        var dict = new Dictionary<Name, IPdfObject>
        {
            { DictionaryKeys.Size, size },
            { DictionaryKeys.Root, root },
        };

        if (prev != null)
        {
            dict.Add(DictionaryKeys.Prev, prev);
        }

        if (encrypt != null)
        {
            dict.Add(DictionaryKeys.Encrypt, encrypt);
        }

        if (info != null)
        {
            dict.Add(DictionaryKeys.Info, info);
        }

        if (id != null)
        {
            dict.Add(DictionaryKeys.ID, id);
        }

        return new(dict);
    }
}
