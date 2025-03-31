using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.FileStructure.Trailer;

public class TrailerDictionary : Dictionary, ITrailerDictionary
{
    public TrailerDictionary(Dictionary dictionary)
        : base(dictionary) { }

    private TrailerDictionary(Dictionary<Name, IPdfObject> trailerDictionary, IPdfEditor pdfEditor)
        : base(trailerDictionary, pdfEditor) { }

    public Number Size => GetAs<Number>(Constants.DictionaryKeys.Trailer.Size)!;
    public Number? Prev => GetAs<Number>(Constants.DictionaryKeys.Trailer.Prev);
    public IndirectObjectReference? Root => GetAs<IndirectObjectReference>(Constants.DictionaryKeys.Trailer.Root);
    public IndirectObjectReference? Encrypt => GetAs<IndirectObjectReference>(Constants.DictionaryKeys.Trailer.Encrypt);
    public IndirectObjectReference? Info => GetAs<IndirectObjectReference>(Constants.DictionaryKeys.Trailer.Info);
    public ArrayObject? ID => GetAs<ArrayObject>(Constants.DictionaryKeys.Trailer.ID);

    /// <summary>
    /// Create a page from an existing page dictionary.
    /// </summary>
    /// <param name="trailerDictionary">An existing dictionary from which to create the <see cref="TrailerDictionary"/>.</param>
    /// <returns>A <see cref="TrailerDictionary"/> instance.</returns>
    internal static TrailerDictionary FromDictionary(Dictionary trailerDictionary)
    {
        return new TrailerDictionary(trailerDictionary);
    }

    /// <summary>
    /// Create a new <see cref="TrailerDictionary"/>.
    /// </summary>
    internal static TrailerDictionary CreateNew(
        Number size,
        Number? prev,
        IndirectObjectReference root,
        IPdfObject? encrypt,
        IndirectObjectReference? info,
        ArrayObject? id,
        IPdfEditor pdfEditor
        )
    {
        ArgumentNullException.ThrowIfNull(size);
        ArgumentNullException.ThrowIfNull(root);

        var dict = new Dictionary<Name, IPdfObject>
        {
            { Constants.DictionaryKeys.Trailer.Size, size },
            { Constants.DictionaryKeys.Trailer.Root, root },
        };

        if (prev != null)
        {
            dict.Add(Constants.DictionaryKeys.Trailer.Prev, prev);
        }

        if (encrypt != null)
        {
            dict.Add(Constants.DictionaryKeys.Trailer.Encrypt, encrypt);
        }

        if (info != null)
        {
            dict.Add(Constants.DictionaryKeys.Trailer.Info, info);
        }

        if (id != null)
        {
            dict.Add(Constants.DictionaryKeys.Trailer.ID, id);
        }

        return new(dict, pdfEditor);
    }
}
