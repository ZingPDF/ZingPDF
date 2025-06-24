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
    private readonly ParseContext _parseContext = ParseContext.WithOrigin(ObjectOrigin.ParsedDocumentObject);

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

        return io == null ? default : (T)io.Object;
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

    public async Task<IncrementalUpdate?> GenerateUpdateDeltaAsync()
    {
        if (NewOrUpdatedObjects.Count == 0 && _deletedObjects.Count == 0)
        {
            return null;
        }

        IEnumerable<VersionInformation> versions = await _versions;

        var latestVersion = versions.First();

        return new IncrementalUpdate(
            _newObjects,
            _updatedObjects.Values,
            _deletedObjects,
            latestVersion.Trailer,
            latestVersion.CrossReferenceStream
            );
    }

    public Task<DocumentCatalogDictionary> GetDocumentCatalogAsync() => _root.Task;

    //public async Task<ITrailerDictionary> GetTrailerDictionaryAsync() => (await _versions).First().TrailerDictionary;

    public async IAsyncEnumerator<IndirectObject> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        IEnumerable<IndirectObjectId> keys = await GetKeysAsync();

        foreach (var key in keys.Where(k => k.Index > 0))
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

            return await _parserResolver.GetParser<IndirectObject>().ParseAsync(_pdf.Data, _parseContext);
        }

        Logger.Log(LogLevel.Trace, $"{key} is compressed within object stream {xref.Value1}");

        // Resolve the correct object stream that contains the object.
        var (objStreamIndirectObject, adjustedIndex) = await ResolveObjectStreamAsync(new IndirectObjectReference((int)xref.Value1, 0, _parseContext.Origin), xref.Value2);

        var objectStream = (StreamObject<ObjectStreamDictionary>)objStreamIndirectObject.Object;

        // TODO: cache decompressed stream data?
        // Now that we have the correct stream, decompress it.
        Stream decompressedObjectStream = await objectStream.GetDecompressedDataAsync();

        // Read the offset table without unnecessary allocations
        var offsetTableBytes = new byte[objectStream.Dictionary.First];
        await decompressedObjectStream.ReadExactlyAsync(offsetTableBytes, 0, objectStream.Dictionary.First);

        // Decode integer pairs
        var offsets = Encoding.ASCII.GetString(offsetTableBytes)
            .Split([Constants.Characters.Whitespace, .. Constants.EndOfLineCharacters]);

        var objectOffset = Convert.ToInt32(offsets[adjustedIndex * 2 + 1]);

        // The byte offset of an object is relative to the first object.
        // Seek to the object's position and parse it
        decompressedObjectStream.Position = objectStream.Dictionary.First + objectOffset;

        var type = (await _tokenTypeIdentifier.TryIdentifyAsync(decompressedObjectStream))!;

        return new IndirectObject(key.Id, await _parserResolver.GetParserFor(type).ParseAsync(decompressedObjectStream, _parseContext));
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

        // Recurse to resolve the correct object stream.
        return await ResolveObjectStreamAsync(objectStream.Dictionary.Extends, objectIndex);
    }
}
