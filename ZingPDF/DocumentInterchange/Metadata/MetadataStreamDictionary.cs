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
        IPdfContext pdfContext,
        ObjectOrigin objectOrigin
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
            pdfContext,
            objectOrigin
            )
    {
        Set<Name>(Constants.DictionaryKeys.Subtype, "XML");
    }

    protected MetadataStreamDictionary(Dictionary<string, IPdfObject> streamDictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
            : base(streamDictionary, pdfContext, objectOrigin) { }

    new public static MetadataStreamDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
    {
        if (!dictionary.ContainsKey(Constants.DictionaryKeys.Stream.Length))
        {
            throw new ArgumentException("Missing stream Length property.");
        }

        return dictionary is null
            ? throw new ArgumentNullException(nameof(dictionary))
            : new(dictionary, pdfContext, objectOrigin);
    }
}
