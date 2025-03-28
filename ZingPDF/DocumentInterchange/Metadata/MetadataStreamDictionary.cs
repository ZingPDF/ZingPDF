using ZingPDF.IncrementalUpdates;
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
}
