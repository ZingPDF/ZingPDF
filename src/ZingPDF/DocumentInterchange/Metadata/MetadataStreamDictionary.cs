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
        IPdf pdf,
        ObjectContext context
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
            pdf,
            context
            )
    {
        Set<Name>(Constants.DictionaryKeys.Subtype, "XML");
    }

    protected MetadataStreamDictionary(Dictionary<string, IPdfObject> streamDictionary, IPdf pdf, ObjectContext context)
            : base(streamDictionary, pdf, context) { }

    new public static MetadataStreamDictionary FromDictionary(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
    {
        if (!dictionary.ContainsKey(Constants.DictionaryKeys.Stream.Length))
        {
            throw new ArgumentException("Missing stream Length property.");
        }

        return dictionary is null
            ? throw new ArgumentNullException(nameof(dictionary))
            : new(dictionary, pdf, context);
    }
}
