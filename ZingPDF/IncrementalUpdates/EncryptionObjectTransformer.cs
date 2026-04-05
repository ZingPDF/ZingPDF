using ZingPDF.Syntax.Encryption;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.IncrementalUpdates;

internal static class EncryptionObjectTransformer
{
    public static async Task<IndirectObject> EncryptAsync(IndirectObject source, StandardSecurityHandler handler)
        => new(source.Id, await TransformObjectAsync(source.Object, source.Id, handler, encryptStrings: true, decryptParsedStreams: false));

    public static async Task<IndirectObject> DecryptAsync(IndirectObject source, StandardSecurityHandler handler)
        => new(source.Id, await TransformObjectAsync(source.Object, source.Id, handler, encryptStrings: false, decryptParsedStreams: true));

    private static async Task<IPdfObject> TransformObjectAsync(
        IPdfObject source,
        IndirectObjectId objectId,
        StandardSecurityHandler handler,
        bool encryptStrings,
        bool decryptParsedStreams)
    {
        switch (source)
        {
            case PdfString pdfString when encryptStrings:
                return PdfString.FromBytes(handler.Transform(objectId, pdfString.Bytes, null, encrypt: true), pdfString.Syntax, source.Context);

            case IStreamObject streamObject:
                return await TransformStreamAsync(streamObject, objectId, handler, encryptStrings, decryptParsedStreams);

            case Dictionary dictionary:
                return await TransformDictionaryAsync(dictionary, objectId, handler, encryptStrings, decryptParsedStreams);

            case ArrayObject array:
                return await TransformArrayAsync(array, objectId, handler, encryptStrings, decryptParsedStreams);

            default:
                return (IPdfObject)source.Clone();
        }
    }

    private static async Task<Dictionary> TransformDictionaryAsync(
        Dictionary source,
        IndirectObjectId objectId,
        StandardSecurityHandler handler,
        bool encryptStrings,
        bool decryptParsedStreams)
    {
        var clone = new Dictionary(source.Pdf, source.Context);

        foreach (var entry in source)
        {
            clone.Set(entry.Key, await TransformObjectAsync(entry.Value, objectId, handler, encryptStrings, decryptParsedStreams));
        }

        return clone;
    }

    private static async Task<ArrayObject> TransformArrayAsync(
        ArrayObject source,
        IndirectObjectId objectId,
        StandardSecurityHandler handler,
        bool encryptStrings,
        bool decryptParsedStreams)
    {
        var clone = new ArrayObject([], source.Context);

        foreach (var item in source)
        {
            clone.Add(await TransformObjectAsync(item, objectId, handler, encryptStrings, decryptParsedStreams));
        }

        return clone;
    }

    private static async Task<IStreamObject> TransformStreamAsync(
        IStreamObject source,
        IndirectObjectId objectId,
        StandardSecurityHandler handler,
        bool encryptStrings,
        bool decryptParsedStreams)
    {
        var transformedDictionary = StreamDictionary.FromDictionary(
            (Dictionary)await TransformDictionaryAsync((Dictionary)source.Dictionary, objectId, handler, encryptStrings, decryptParsedStreams));

        var bytes = await ReadStreamBytesAsync(source.Data);

        if (encryptStrings && handler.ShouldDecrypt(objectId, transformedDictionary))
        {
            bytes = handler.Transform(objectId, bytes, transformedDictionary, encrypt: true);
        }
        else if (decryptParsedStreams && source.Context.Origin == ObjectOrigin.ParsedDocumentObject && handler.ShouldDecrypt(objectId, transformedDictionary))
        {
            bytes = handler.Transform(objectId, bytes, transformedDictionary, encrypt: false);
        }

        return new StreamObject<IStreamDictionary>(new MemoryStream(bytes), transformedDictionary);
    }

    private static async Task<byte[]> ReadStreamBytesAsync(Stream source)
    {
        source.Position = 0;

        if (source.CanSeek && source.Length <= int.MaxValue)
        {
            var length = (int)source.Length;
            if (length == 0)
            {
                return [];
            }

            var bytes = new byte[length];
            await source.ReadExactlyAsync(bytes, 0, length);
            return bytes;
        }

        using var output = new MemoryStream();
        await source.CopyToAsync(output);
        return output.ToArray();
    }
}
