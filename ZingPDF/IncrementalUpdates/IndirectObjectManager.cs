using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.IncrementalUpdates;

/// <summary>
/// <see cref="IIndirectObjectDictionary"/> containing all of the PDFs indirect objects.<para></para>
/// Allows adding, removing, and updating of objects.
/// </summary>
public class IndirectObjectManager : IIndirectObjectDictionary
{
    private readonly IIndirectObjectDictionary _sourceDictionary;
    private readonly Queue<IndirectObjectId> _freeIds;

    public IndirectObjectManager(IIndirectObjectDictionary sourceDictionary)
    {
        _sourceDictionary = sourceDictionary ?? throw new ArgumentNullException(nameof(sourceDictionary));

        _freeIds = new Queue<IndirectObjectId>(_sourceDictionary.GetFreeIds());
    }

    public int Count => _sourceDictionary.Count + NewObjects.Count - DeletedObjects.Count;

    public List<IndirectObject> NewObjects { get; } = [];
    public Dictionary<IndirectObjectId, IndirectObject> UpdatedObjects { get; } = [];
    public List<IndirectObjectId> DeletedObjects { get; } = [];

    public HashSet<IndirectObject> NewOrUpdatedObjects
    {
        get
        {
            var objects = new HashSet<IndirectObject>(UpdatedObjects.Values);

            foreach (var obj in NewObjects)
            {
                objects.Add(obj);
            }

            return objects;
        }
    }

    public async Task<IndirectObject?> GetAsync(IndirectObjectReference key)
    {
        foreach (var obj in NewOrUpdatedObjects)
        {
            if (obj.Id == key.Id)
            {
                return obj;
            }
        }

        foreach (var obj in DeletedObjects)
        {
            if (obj == key.Id)
            {
                throw new InvalidOperationException($"Unable to dereference indirect object: {key}. Object has been deleted.");
            }
        }

        return await _sourceDictionary.GetAsync(key);
    }

    public async Task<T?> GetAsync<T>(IndirectObjectReference key)
        where T : class, IPdfObject
    {
        var indirectObject = await GetAsync(key);

        return indirectObject?.Object as T;
    }

    public IndirectObject Add(IPdfObject pdfObject)
    {
        ArgumentNullException.ThrowIfNull(pdfObject);

        IndirectObjectId newObjectId = GetNextFreeId();

        var indirectObject = new IndirectObject(newObjectId, pdfObject);

        NewObjects.Add(indirectObject);

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

    public void Update(IndirectObject indirectObject)
    {
        ArgumentNullException.ThrowIfNull(indirectObject);

        UpdatedObjects[indirectObject.Id] = indirectObject;
    }

    public void Delete(IndirectObjectId indirectObjectId)
    {
        ArgumentNullException.ThrowIfNull(indirectObjectId);

        indirectObjectId.GenerationNumber++;

        DeletedObjects.Add(indirectObjectId);
    }

    public IndirectObjectId GetNextFreeId()
    {
        if (_freeIds.TryDequeue(out var id))
        {
            return id;
        }

        return new IndirectObjectId(Count + 1, 0);
    }

    public List<IndirectObjectId> GetFreeIds()
    {
        throw new NotImplementedException();
    }
}
