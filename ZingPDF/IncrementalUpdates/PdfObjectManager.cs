using Nito.AsyncEx;
using System.Text;
using ZingPDF.Logging;
using ZingPDF.Parsing;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.IncrementalUpdates;

/// <summary>
/// Master class for retrieving and mutating objects in a PDF.
/// </summary>
/// <remarks>
/// This class maintains a collection of PDF versions, with each version containing the index for objects created or modified in that version.
/// </remarks>
public record PdfObjectManager : IPdfEditor, IAsyncEnumerable<IndirectObject>
{
    private readonly Dictionary<IndirectObjectId, IndirectObject> _parsedObjectCache = [];
    private readonly Stream _pdfInputStream;

    private readonly IEnumerable<VersionInformation> _versions;

    private readonly List<IndirectObject> _newObjects = [];
    private readonly Dictionary<IndirectObjectId, IndirectObject> _updatedObjects = [];
    private readonly List<IndirectObjectId> _deletedObjects = [];

    private readonly AsyncLazy<DocumentCatalogDictionary> _root;

    //private readonly Queue<IndirectObjectId> _freeIds;

    public PdfObjectManager(Stream pdfInputStream, IEnumerable<VersionInformation> versions)
    {
        ArgumentNullException.ThrowIfNull(versions, nameof(versions));
        _pdfInputStream = pdfInputStream;
        _versions = versions;

        //_freeIds = new Queue<IndirectObjectId>(GetFreeIds());

        _root = new AsyncLazy<DocumentCatalogDictionary>(async () =>
        {
            // The root property is copied from trailer to trailer during updates.
            // Find the first non-null property.
            // TODO: can the root reference change during an update? How do we ensure this is the latest?
            var catalogRef = _versions.FirstOrDefault(v => v.TrailerDictionary.Root != null)?.TrailerDictionary.Root
                ?? throw new InvalidPdfException("Missing Root entry");

            return (await GetAsync(catalogRef))?.Object as DocumentCatalogDictionary
                ?? throw new InvalidPdfException("Unable to dereference document catalog");
        });
    }

    public HashSet<IndirectObject> NewOrUpdatedObjects
    {
        get
        {
            var objects = new HashSet<IndirectObject>(_updatedObjects.Values);

            foreach (var obj in _newObjects)
            {
                objects.Add(obj);
            }

            return objects;
        }
    }

    public int Count => _versions.Sum(v => v.IndirectObjects.Count) + _newObjects.Count - _deletedObjects.Count;

    public IEnumerable<IndirectObjectId> Keys
        => _versions
            .SelectMany(v => v.IndirectObjects.Keys)
            .Concat(_newObjects.Select(x => x.Id));

    public bool ContainsKey(IndirectObjectReference key) => _versions.Any(v => v.IndirectObjects.ContainsKey(key.Id));

    public async Task<IndirectObject> GetAsync(IndirectObjectReference key)
    {
        // First check new/updated and deleted objects
        foreach (var obj in NewOrUpdatedObjects)
        {
            if (obj.Id == key.Id)
            {
                return obj;
            }
        }

        foreach (var obj in _deletedObjects)
        {
            if (obj == key.Id)
            {
                throw new InvalidOperationException($"Unable to dereference indirect object: {key}. Object has been deleted.");
            }
        }

        // Then check the local object cache.
        if (_parsedObjectCache.TryGetValue(key.Id, out var cachedObj))
        {
            return cachedObj;
        }

        // Finally, parse and cache the value.
        // Search through versions, which are ordered most recent first.
        foreach (var version in _versions)
        {
            if (version.IndirectObjects.TryGetValue(key.Id, out var entry))
            {
                IndirectObject obj = await DereferenceObjectAsync(key, entry);

                _parsedObjectCache.Add(key.Id, obj);

                return obj;
            }
        }

        throw new InvalidOperationException($"Unable to dereference indirect object: {key}.");
    }

    public async Task<T?> GetAsync<T>(IndirectObjectReference key) where T : class, IPdfObject
    {
        var io = await GetAsync(key);

        return io == null ? default : (T)io.Object;
    }

    public IndirectObject Add(IPdfObject pdfObject)
    {
        ArgumentNullException.ThrowIfNull(pdfObject);

        IndirectObjectId newObjectId = GetNextFreeId();

        var indirectObject = new IndirectObject(newObjectId, pdfObject);

        _newObjects.Add(indirectObject);

        return indirectObject;
    }

    public void AddRange(IEnumerable<IPdfObject> pdfObjects)
    {
        ArgumentNullException.ThrowIfNull(pdfObjects);

        foreach (var pdfObject in pdfObjects)
        {
            _ = Add(pdfObject);
        }
    }

    public void Delete(IndirectObjectId indirectObjectId)
    {
        ArgumentNullException.ThrowIfNull(indirectObjectId);

        indirectObjectId.GenerationNumber++;

        _deletedObjects.Add(indirectObjectId);
    }

    public void Update(IndirectObject indirectObject)
    {
        ArgumentNullException.ThrowIfNull(indirectObject);

        _updatedObjects[indirectObject.Id] = indirectObject;
    }

    public async Task<IncrementalUpdate?> GenerateUpdateDeltaAsync()
    {
        if (NewOrUpdatedObjects.Count == 0 && _deletedObjects.Count == 0)
        {
            return null;
        }

        var latestVersion = _versions.First();

        return new IncrementalUpdate(
            this,
            _newObjects,
            _updatedObjects.Values,
            _deletedObjects,
            latestVersion.Trailer,
            latestVersion.CrossReferenceStream
            );
    }

    public Task<DocumentCatalogDictionary> GetDocumentCatalogAsync() => _root.Task;

    public ITrailerDictionary GetTrailerDictionary() => _versions.First().TrailerDictionary;

    public async IAsyncEnumerator<IndirectObject> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        foreach (var key in Keys.Where(k => k.Index > 0))
        {
            yield return await GetAsync(key.Reference);
        }
    }

    //private static List<IndirectObjectId> GetFreeIds()
    //{
    //    // TODO: it might be more efficient to traverse the linked list of free entries here.

    //    return _xrefs.Where(x => !x.Value.InUse)
    //        .Select(x => new IndirectObjectId(x.Key, x.Value.Value2))
    //        .ToList();
    //}

    private IndirectObjectId GetNextFreeId()
    {
        //if (_freeIds.TryDequeue(out var id))
        //{
        //    return id;
        //}

        // TODO: efficiently grab a free ID from deleted objects if present

        var highestIndex = Keys.Max(k => k.Index);

        return new IndirectObjectId(highestIndex + 1, 0);
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
        Stream decompressedObjectStream = await objectStream.GetDecompressedDataAsync();
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

        return new IndirectObject(key.Id, await Parser.For(type, this).ParseAsync(decompressedObjectStream));
    }
}
