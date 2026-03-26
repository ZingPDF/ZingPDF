using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx;
using System.Text;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Logging;
using ZingPDF.Parsing;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Parsing.Parsers.FileStructure;
using ZingPDF.Parsing.Parsers.Objects;
using ZingPDF.Syntax;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF;

/// <summary>
/// Master class for retrieving and mutating objects in a PDF.
/// </summary>
/// <remarks>
/// This class maintains a collection of PDF versions, with each version containing the index for objects created or modified in that version.
/// </remarks>
public class PdfObjectCollection : IPdfObjectCollection, IAsyncEnumerable<IndirectObject>
{
    private const int MaxCachedObjectStreamBytes = 4 * 1024 * 1024;
    private const int MaxTotalCachedObjectStreamBytes = 32 * 1024 * 1024;

    private readonly ObjectContext _ObjectContext = ObjectContext.WithOrigin(ObjectOrigin.ParsedDocumentObject);

    private readonly Dictionary<IndirectObjectId, IndirectObject> _parsedObjectCache = [];
    
    private readonly List<IndirectObject> _newObjects = [];
    private readonly Dictionary<IndirectObjectId, IndirectObject> _updatedObjects = [];
    private readonly List<IndirectObjectId> _deletedObjects = [];

    private readonly AsyncLazy<IEnumerable<VersionInformation>> _versions;
    private readonly AsyncLazy<DocumentCatalogDictionary> _root;

    private readonly IDocumentVersionParser _documentVersionParser;
    private readonly IParserResolver _parserResolver;
    private readonly ITokenTypeIdentifier _tokenTypeIdentifier;
    private readonly IPdf _pdf;
    private readonly BoundedObjectStreamCache _objectStreamCache = new(MaxTotalCachedObjectStreamBytes, MaxCachedObjectStreamBytes);

    //private readonly Queue<IndirectObjectId> _freeIds;

    public PdfObjectCollection(
        IPdf pdf,
        IDocumentVersionParser documentVersionParser,
        IParserResolver parserResolver,
        ITokenTypeIdentifier tokenTypeIdentifier
        )
    {
        _pdf = pdf;
        _documentVersionParser = documentVersionParser;
        _parserResolver = parserResolver;
        _tokenTypeIdentifier = tokenTypeIdentifier;
        _versions = new AsyncLazy<IEnumerable<VersionInformation>>(async () => await _documentVersionParser.ParseAsync(_pdf.Data));

        //_freeIds = new Queue<IndirectObjectId>(GetFreeIds());

        _root = new AsyncLazy<DocumentCatalogDictionary>(async () =>
        {
            IEnumerable<VersionInformation> versions = await _versions;

            // The root property is copied from trailer to trailer during updates.
            // Find the first non-null property.
            // TODO: can the root reference change during an update? How do we ensure this is the latest?
            var catalogRef = versions.FirstOrDefault(v => v.TrailerDictionary.Root != null)?.TrailerDictionary.Root
                ?? throw new InvalidPdfException("Missing Root entry");

            return (await GetAsync(catalogRef))?.Object as DocumentCatalogDictionary
                ?? throw new InvalidPdfException("Unable to dereference document catalog");
        });

        PageTree = new PageTree(this);
    }

    public PageTree PageTree { get; }

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

    public async Task<int> GetCountAsync() => (await _versions).Sum(v => v.IndirectObjects.Count) + _newObjects.Count - _deletedObjects.Count;

    public async Task<IEnumerable<IndirectObjectId>> GetKeysAsync()
        => (await _versions)
            .SelectMany(v => v.IndirectObjects.Keys)
            .Concat(_newObjects.Select(x => x.Id));

    public async Task<bool> ContainsKeyAsync(IndirectObjectReference key) => (await _versions).Any(v => v.IndirectObjects.ContainsKey(key.Id));

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

        IEnumerable<VersionInformation> versions = await _versions;

        // Finally, parse and cache the value.
        // Search through versions, which are ordered most recent first.
        foreach (var version in versions)
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

    public async Task<T> GetAsync<T>(IndirectObjectReference key) where T : class?, IPdfObject?
    {
        var io = await GetAsync(key);

        return io.Object as T
            ?? throw new InvalidOperationException($"Indirect object {key} is not of type {typeof(T).Name}.");
    }

    public async Task<IndirectObject> AddAsync(IPdfObject pdfObject)
    {
        ArgumentNullException.ThrowIfNull(pdfObject);

        IndirectObjectId newObjectId = await GetNextFreeIdAsync();

        var indirectObject = new IndirectObject(newObjectId, pdfObject);

        _newObjects.Add(indirectObject);

        return indirectObject;
    }

    public async Task AddRangeAsync(IEnumerable<IPdfObject> pdfObjects)
    {
        ArgumentNullException.ThrowIfNull(pdfObjects);

        foreach (var pdfObject in pdfObjects)
        {
            _ = await AddAsync(pdfObject);
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

    public async Task<IncrementalUpdate?> GenerateUpdateDeltaAsync(bool includeAllObjects = false)
    {
        if (!includeAllObjects && NewOrUpdatedObjects.Count == 0 && _deletedObjects.Count == 0)
        {
            return null;
        }

        IEnumerable<VersionInformation> versions = await _versions;

        var latestVersion = versions.First();

        IEnumerable<IndirectObject> updatedObjects = _updatedObjects.Values;

        if (includeAllObjects)
        {
            var allObjects = new List<IndirectObject>();
            await foreach (var obj in this)
            {
                allObjects.Add(obj);
            }

            updatedObjects = allObjects;
        }

        return new IncrementalUpdate(
            _newObjects,
            updatedObjects,
            _deletedObjects,
            latestVersion.Trailer,
            latestVersion.CrossReferenceStream
            );
    }

    public Task<DocumentCatalogDictionary> GetDocumentCatalogAsync() => _root.Task;

    public async Task<ITrailerDictionary> GetLatestTrailerDictionaryAsync()
        => (await _versions).First().TrailerDictionary;

    public async IAsyncEnumerator<IndirectObject> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var liveKeys = (await _versions)
            .SelectMany(v => v.IndirectObjects)
            .Where(kvp => kvp.Key.Index > 0 && kvp.Value.InUse)
            .Select(kvp => kvp.Key)
            .Concat(_newObjects.Select(x => x.Id))
            .Distinct();

        foreach (var key in liveKeys)
        {
            yield return await GetAsync(new IndirectObjectReference(key));
        }
    }

    //private static List<IndirectObjectId> GetFreeIds()
    //{
    //    // TODO: it might be more efficient to traverse the linked list of free entries here.

    //    return _xrefs.Where(x => !x.Value.InUse)
    //        .Select(x => new IndirectObjectId(x.Key, x.Value.Value2))
    //        .ToList();
    //}

    private async Task<IndirectObjectId> GetNextFreeIdAsync()
    {
        //if (_freeIds.TryDequeue(out var id))
        //{
        //    return id;
        //}

        // TODO: efficiently grab a free ID from deleted objects if present

        var keys = await GetKeysAsync();

        var highestIndex = keys.Max(k => k.Index);

        return new IndirectObjectId(highestIndex + 1, 0);
    }

    private async Task<IndirectObject> DereferenceObjectAsync(IndirectObjectReference key, CrossReferenceEntry xref)
    {
        if (!xref.Compressed)
        {
            _pdf.Data.Position = xref.Value1;

            return await _parserResolver.GetParser<IndirectObject>().ParseAsync(_pdf.Data, _ObjectContext);
        }

        Logger.Log(LogLevel.Trace, $"{key} is compressed within object stream {xref.Value1}");

        // Resolve the correct object stream that contains the object.
        var (objStreamIndirectObject, adjustedIndex) = await ResolveObjectStreamAsync(new IndirectObjectReference((int)xref.Value1, 0, _ObjectContext), xref.Value2);

        var objectStream = (StreamObject<ObjectStreamDictionary>)objStreamIndirectObject.Object;

        if (_objectStreamCache.TryGet(objStreamIndirectObject.Id, out var cachedObjectStream))
        {
            using var cachedStream = cachedObjectStream.OpenStream();
            return await ParseCompressedObjectAsync(key, objStreamIndirectObject.Id, objectStream.Dictionary.First, cachedObjectStream.ObjectOffsets, adjustedIndex, cachedStream);
        }

        using var decompressedObjectStream = await objectStream.GetDecompressedDataAsync();

        int firstObjectOffset = objectStream.Dictionary.First;
        var offsetTableBytes = new byte[firstObjectOffset];
        await decompressedObjectStream.ReadExactlyAsync(offsetTableBytes, 0, firstObjectOffset);
        var objectOffsets = ParseObjectOffsets(offsetTableBytes, objectStream.Dictionary.N);

        if (ShouldCacheObjectStream(decompressedObjectStream, objectOffsets.Length))
        {
            decompressedObjectStream.Position = 0;
            using var buffer = new MemoryStream((int)decompressedObjectStream.Length);
            await decompressedObjectStream.CopyToAsync(buffer);

            var cachedEntry = new ObjectStreamData(buffer.ToArray(), objectOffsets);
            _objectStreamCache.Store(objStreamIndirectObject.Id, cachedEntry);

            using var cachedStream = cachedEntry.OpenStream();
            return await ParseCompressedObjectAsync(key, objStreamIndirectObject.Id, firstObjectOffset, objectOffsets, adjustedIndex, cachedStream);
        }

        return await ParseCompressedObjectAsync(key, objStreamIndirectObject.Id, firstObjectOffset, objectOffsets, adjustedIndex, decompressedObjectStream);
    }

    private async Task<(IndirectObject, int)> ResolveObjectStreamAsync(IndirectObjectReference objectStreamRef, int objectIndex)
    {
        var objStreamIndirectObject = await GetAsync(objectStreamRef)
            ?? throw new InvalidOperationException($"Unable to find parent object stream {objectStreamRef}");

        var objectStream = (StreamObject<ObjectStreamDictionary>)objStreamIndirectObject.Object;

        // If the object index is within bounds, return the current stream.
        if (objectIndex < objectStream.Dictionary.N)
            return (objStreamIndirectObject, objectIndex);

        // Otherwise, check the Extends reference.
        if (objectStream.Dictionary.Extends is null)
            throw new InvalidOperationException($"Requested object index {objectIndex} is out of bounds, and no Extends reference exists.");

        // Object indexes in an extended stream are relative to the full chain,
        // so we need to subtract this segment's object count before recursing.
        return await ResolveObjectStreamAsync(objectStream.Dictionary.Extends, objectIndex - objectStream.Dictionary.N);
    }

    private async Task<IndirectObject> ParseCompressedObjectAsync(
        IndirectObjectReference key,
        IndirectObjectId objectStreamId,
        int firstObjectOffset,
        int[] objectOffsets,
        int adjustedIndex,
        Stream decompressedObjectStream)
    {
        if ((uint)adjustedIndex >= (uint)objectOffsets.Length)
            throw new InvalidOperationException($"Object stream {objectStreamId} does not contain index {adjustedIndex}.");

        // The byte offset of an object is relative to the first object.
        decompressedObjectStream.Position = firstObjectOffset + objectOffsets[adjustedIndex];

        var type = (await _tokenTypeIdentifier.TryIdentifyAsync(decompressedObjectStream))!;
        var itemContext = _ObjectContext with { NearestParent = new IndirectObjectReference(key.Id, _ObjectContext) };
        return new IndirectObject(key.Id, await _parserResolver.GetParserFor(type).ParseAsync(decompressedObjectStream, itemContext));
    }

    private static bool ShouldCacheObjectStream(Stream decompressedObjectStream, int objectCount)
    {
        if (!decompressedObjectStream.CanSeek)
            return false;

        long estimatedEntrySize = decompressedObjectStream.Length + ((long)objectCount * sizeof(int));
        return estimatedEntrySize <= MaxCachedObjectStreamBytes;
    }

    private static int[] ParseObjectOffsets(byte[] offsetTableBytes, int objectCount)
    {
        var offsets = new int[objectCount];
        var table = Encoding.ASCII.GetString(offsetTableBytes);
        int tokenIndex = 0;
        int objectIndex = 0;

        foreach (var token in table.Split([Constants.Characters.Whitespace, .. Constants.EndOfLineCharacters], StringSplitOptions.RemoveEmptyEntries))
        {
            if ((tokenIndex & 1) == 1)
            {
                if (objectIndex >= objectCount)
                    break;

                offsets[objectIndex++] = Convert.ToInt32(token);
            }

            tokenIndex++;
        }

        if (objectIndex != objectCount)
            throw new InvalidPdfException("Object stream offset table is malformed.");

        return offsets;
    }

    private sealed class ObjectStreamData(byte[] data, int[] objectOffsets)
    {
        public byte[] Data { get; } = data;
        public int[] ObjectOffsets { get; } = objectOffsets;
        public int SizeBytes => Data.Length + (ObjectOffsets.Length * sizeof(int));

        public MemoryStream OpenStream() => new(Data, writable: false);
    }

    private sealed class BoundedObjectStreamCache(int maxTotalBytes, int maxEntryBytes)
    {
        private readonly int _maxTotalBytes = maxTotalBytes;
        private readonly int _maxEntryBytes = maxEntryBytes;
        private readonly object _gate = new();
        private readonly Dictionary<IndirectObjectId, LinkedListNode<CacheEntry>> _entries = [];
        private readonly LinkedList<CacheEntry> _lru = [];
        private int _cachedBytes;

        public bool TryGet(IndirectObjectId objectStreamId, out ObjectStreamData data)
        {
            lock (_gate)
            {
                if (_entries.TryGetValue(objectStreamId, out var node))
                {
                    _lru.Remove(node);
                    _lru.AddFirst(node);
                    data = node.Value.Data;
                    return true;
                }
            }

            data = null!;
            return false;
        }

        public void Store(IndirectObjectId objectStreamId, ObjectStreamData data)
        {
            if (data.SizeBytes > _maxEntryBytes)
                return;

            lock (_gate)
            {
                if (_entries.TryGetValue(objectStreamId, out var existing))
                {
                    _cachedBytes -= existing.Value.Data.SizeBytes;
                    _lru.Remove(existing);
                    _entries.Remove(objectStreamId);
                }

                while (_cachedBytes + data.SizeBytes > _maxTotalBytes && _lru.Last is not null)
                {
                    var last = _lru.Last;
                    _cachedBytes -= last.Value.Data.SizeBytes;
                    _entries.Remove(last.Value.ObjectStreamId);
                    _lru.RemoveLast();
                }

                var node = new LinkedListNode<CacheEntry>(new CacheEntry(objectStreamId, data));
                _lru.AddFirst(node);
                _entries[objectStreamId] = node;
                _cachedBytes += data.SizeBytes;
            }
        }

        private sealed class CacheEntry(IndirectObjectId objectStreamId, ObjectStreamData data)
        {
            public IndirectObjectId ObjectStreamId { get; } = objectStreamId;
            public ObjectStreamData Data { get; } = data;
        }
    }
}
