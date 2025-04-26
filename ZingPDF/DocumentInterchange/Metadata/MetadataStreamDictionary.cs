using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.DocumentInterchange.Metadata;

public class MetadataStreamDictionary : StreamDictionary
{
    public MetadataStreamDictionary(
        Number length,
        ShorthandArrayObject? filter,
        ShorthandArrayObject? decodeParms,
        Dictionary? f,
        ShorthandArrayObject? fFilter,
        ShorthandArrayObject? fDecodeParms,
        Number? dL,
        IPdfEditor pdfEditor
        )
        : base(
            Constants.DictionaryTypes.Metadata,
            length,
            filter,
            decodeParms,
            f,
            fFilter,
            fDecodeParms,
            dL,
            pdfEditor
            )
    {
        Set<Name>(Constants.DictionaryKeys.Subtype, "XML");
    }

    protected MetadataStreamDictionary(Dictionary<Name, IPdfObject> streamDictionary, IPdfEditor pdfEditor)
            : base(streamDictionary, pdfEditor) { }

    new public static MetadataStreamDictionary FromDictionary(Dictionary<Name, IPdfObject> dictionary, IPdfEditor pdfEditor)
    {
        if (!dictionary.ContainsKey(Constants.DictionaryKeys.Stream.Length))
        {
            throw new ArgumentException("Missing stream Length property.");
        }

        return dictionary is null
            ? throw new ArgumentNullException(nameof(dictionary))
            : new(dictionary, pdfEditor);
    }
}
