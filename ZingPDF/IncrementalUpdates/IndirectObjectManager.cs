using ZingPDF.ObjectModel;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF.IncrementalUpdates;

internal class IndirectObjectManager : IIndirectObjectDictionary
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

    public List<IndirectObject> NewOrUpdatedObjects { get => UpdatedObjects.Values.Concat(NewObjects).ToList(); }

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
                return null;
            }
        }

        return await _sourceDictionary.GetAsync(key);
    }

    public async Task<T?> GetAsync<T>(IndirectObjectReference key)
    {
        var indirectObject = await GetAsync(key)
            ?? throw new InvalidOperationException($"Unable to find indirect object from reference: {key}");

        return indirectObject.Get<T>();
    }

    public IndirectObject Add(IPdfObject pdfObject)
    {
        ArgumentNullException.ThrowIfNull(pdfObject);

        IndirectObjectId newObjectId = GetNextFreeId();

        var indirectObject = new IndirectObject(newObjectId, pdfObject);

        NewObjects.Add(indirectObject);

        return indirectObject;
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
