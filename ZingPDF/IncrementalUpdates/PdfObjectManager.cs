using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.IncrementalUpdates;

public record PdfObjectManager : IIndirectObjectDictionary, IPdfEditor
{
    private readonly IEnumerable<VersionInformation> _versions;

    private readonly List<IndirectObject> _newObjects = [];
    private readonly Dictionary<IndirectObjectId, IndirectObject> _updatedObjects = [];
    private readonly List<IndirectObjectId> _deletedObjects = [];

    //private readonly Queue<IndirectObjectId> _freeIds;

    public PdfObjectManager(IEnumerable<VersionInformation> versions)
    {
        ArgumentNullException.ThrowIfNull(versions, nameof(versions));

        _versions = versions;

        //_freeIds = new Queue<IndirectObjectId>(GetFreeIds());
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

    public bool ContainsKey(IndirectObjectReference key) => _versions.Any(v => v.IndirectObjects.ContainsKey(key));

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

        foreach(var version in _versions)
        {
            if (version.IndirectObjects.ContainsKey(key))
            {
                return await version.IndirectObjects.GetAsync(key);
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

        List<CrossReferenceSection> xrefSections = CrossReferenceGenerator.Generate(NewOrUpdatedObjects, _deletedObjects);

        var xrefTable = new CrossReferenceTable(xrefSections);

        var latestVersion = _versions.First();

        // The prev value points to the previous latest xref table or stream.
        // If the current PDF has a trailer, prev should be the same as the current startxref value.
        // If the current PDF instead uses an xref stream dictionary, prev is going to be the offset of the stream dictionary
        long prev = latestVersion.Trailer?.XrefTableByteOffset ?? latestVersion.CrossReferenceStream!.ByteOffset!.Value;

        // Build file identifier
        var originalId = latestVersion.TrailerDictionary.ID?[0] ?? HexadecimalString.FromBytes(Guid.NewGuid().ToByteArray());
        var updateId = HexadecimalString.FromBytes(Guid.NewGuid().ToByteArray());
        var fileIdentifier = new ArrayObject([originalId, updateId]);

        var trailer = new Trailer(
            TrailerDictionary.CreateNew(
                Count,
                prev,
                latestVersion.TrailerDictionary.Root, // TODO: figure out how best to handle this if it can be null
                latestVersion.TrailerDictionary.Encrypt,
                latestVersion.TrailerDictionary.Info,
                fileIdentifier
                ),
            xrefTable.ByteOffset!.Value
            );

        return new IncrementalUpdate(trailer, xrefTable, NewOrUpdatedObjects);
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

        return new IndirectObjectId(Count + 1, 0);
    }
}
