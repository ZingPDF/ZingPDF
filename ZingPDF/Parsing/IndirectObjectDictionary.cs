using System.Text;
using ZingPDF.Logging;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Parsing;

internal class IndirectObjectDictionary : IIndirectObjectDictionary
{
    private readonly Dictionary<IndirectObjectId, IndirectObject> _parsedObjectCache = [];

    private readonly Stream _pdfInputStream;
    private readonly Dictionary<int, CrossReferenceEntry> _xrefs;

    public IndirectObjectDictionary(Stream pdfInputStream, Dictionary<int, CrossReferenceEntry> xrefs)
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));
        ArgumentNullException.ThrowIfNull(xrefs, nameof(xrefs));

        _pdfInputStream = pdfInputStream;
        _xrefs = xrefs;
    }

    public int Count => _xrefs.Count(x => x.Value.InUse); // + _newObjects.Count - _deletedObjects.Count;

    public bool ContainsKey(IndirectObjectReference key) => _xrefs.Any(x => x.Key == key.Id.Index);

    public async Task<IndirectObject> GetAsync(IndirectObjectReference key)
    {
        // Check the local object cache first.
        if (_parsedObjectCache.TryGetValue(key.Id, out var cachedObj))
        {
            return cachedObj;
        }

        if (_xrefs.TryGetValue(key.Id.Index, out var cachedXrefEntry))
        {
            IndirectObject obj = await DereferenceObjectAsync(key, cachedXrefEntry);

            _parsedObjectCache.Add(key.Id, obj);

            return obj;
        }

        throw new InvalidOperationException($"Unable to dereference indirect object: {key}");
    }

    public async Task<T?> GetAsync<T>(IndirectObjectReference key) where T : class, IPdfObject
    {
        var io = await GetAsync(key);

        return io == null ? default : (T)io.Object;
    }

    private async Task<IndirectObject> DereferenceObjectAsync(IndirectObjectReference key, CrossReferenceEntry xref)
    {
        if (!xref.Compressed)
        {
            _pdfInputStream.Position = xref.Value1;

            return await Parser.For<IndirectObject>(this).ParseAsync(_pdfInputStream);
        }

        Logger.Log(LogLevel.Trace, $"{key} is compressed within object stream {xref.Value1}");

        // TODO: must support the `Extends` property

        var objStreamIndirectObject = await GetAsync(new IndirectObjectReference(new IndirectObjectId((int)xref.Value1, 0)))
            ?? throw new InvalidOperationException($"Error attempting to parse {key}. Unable to find parent object stream {xref.Value1}");

        var objectStream = (StreamObject<IStreamDictionary>)objStreamIndirectObject.Object;
        var objectStreamDictionary = (objectStream.Dictionary as ObjectStreamDictionary)!;

        // TODO: cache decompressed stream data?
        // Decompress stream, read bytes up to first object.
        // These bytes contain pairs of integers, identifying each object number and byte offset.          
        Stream decompressedObjectStream = await objectStream.GetDecompressedDataAsync(this);
        var decompressedData = new byte[objectStreamDictionary.First];
        await decompressedObjectStream.ReadExactlyAsync(decompressedData, 0, objectStreamDictionary.First);

        // Decode integer pairs
        var offsets = Encoding.ASCII.GetString(decompressedData)
            .Split([Constants.Whitespace, .. Constants.EndOfLineCharacters]);

        var indexedOffsets = new int[objectStreamDictionary.N];

        for (var i = 0; i < objectStreamDictionary.N; i++)
        {
            var byteOffset = Convert.ToInt32(offsets[i * 2 + 1]);

            indexedOffsets[i] = byteOffset;
        }

        var objectOffset = indexedOffsets[xref.Value2];

        // The byte offset of an object is relative to the first object.
        decompressedObjectStream.Position = objectStreamDictionary.First + objectOffset;

        var type = (await TokenTypeIdentifier.TryIdentifyAsync(decompressedObjectStream))!;

        return new IndirectObject(key.Id, await Parser.For(type).ParseAsync(decompressedObjectStream));
    }
}