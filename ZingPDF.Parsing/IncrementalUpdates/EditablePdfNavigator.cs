using ZingPDF.Extensions;
using ZingPDF.Objects.ObjectGroups.CrossReferences;
using ZingPDF.Objects.ObjectGroups.Trailer;
using ZingPDF.Objects;
using ZingPDF.Objects.Primitives.IndirectObjects;

namespace ZingPDF.Parsing.IncrementalUpdates;

/// <summary>
/// IPdfNavigator which aggregates elements from a root PDF file, any incremental updates, plus unsaved objects.
/// </summary>
internal class EditablePdfNavigator : IPdfNavigator
{
    // Navigator for the root PDF file
    private readonly PdfFileNavigator _pdfFileNavigator;

    public EditablePdfNavigator(PdfFileNavigator pdfFileNavigator)
    {
        _pdfFileNavigator = pdfFileNavigator ?? throw new ArgumentNullException(nameof(pdfFileNavigator));
    }

    public List<IncrementalUpdate> Updates { get; } = [];

    public IncrementalUpdate GetWorkingIncrementalUpdate()
    {
        var latestIncrementalUpdate = Updates.SingleOrDefault(x => !x.Written);
        if (latestIncrementalUpdate is null)
        {
            latestIncrementalUpdate = new IncrementalUpdate(this);
            Updates.Add(latestIncrementalUpdate);
        }

        return latestIncrementalUpdate;
    }

    public bool UsingXrefStreams => _pdfFileNavigator.UsingXrefStreams;
    public bool UsingXrefTables => _pdfFileNavigator.UsingXrefTables;

    public async Task<IndirectObject> DereferenceIndirectObjectAsync(IndirectObjectReference reference)
    {
        // Search backwards through all updated objects for the specified reference
        for (int i = Updates.Count - 1; i >= 0; i--)
        {
            var update = Updates[i];

            foreach (var obj in update.UpdatedObjects)
            {
                if (obj.Key == reference.Id)
                {
                    return obj.Value;
                }
            }
        }

        return await _pdfFileNavigator.DereferenceIndirectObjectAsync(reference);
    }

    public async Task<Dictionary<int, CrossReferenceEntry>> GetAggregateCrossReferencesAsync()
    {
        var latestIncrementalUpdate = GetWorkingIncrementalUpdate();

        // Concatenate unsaved entries with existing objects
        var updateDictionary = latestIncrementalUpdate.NewOrUpdatedObjects
            .ToDictionary(e => e.Id.Index, e => new CrossReferenceEntry(0, 0, inUse: true, compressed: false));

        var existingXrefs = await _pdfFileNavigator.GetAggregateCrossReferencesAsync();

        updateDictionary.MergeInto(await _pdfFileNavigator.GetAggregateCrossReferencesAsync());

        return existingXrefs;
    }

    public Task<LinearizationDictionary?> GetLinearizationDictionaryAsync()
    {
        return _pdfFileNavigator.GetLinearizationDictionaryAsync();
    }

    public async Task<IEnumerable<IndirectObject>> GetPagesAsync()
    {
        var originalPages = await _pdfFileNavigator.GetPagesAsync();

        var newOrUpdatedPages = Updates
            .SelectMany(x => x.NewOrUpdatedObjects);

        // TODO: ensure new pages override old ones
        return originalPages.Except(newOrUpdatedPages);
    }

    public async Task<IndirectObject> GetRootPageTreeNodeAsync()
    {
        var rootPageTreeNode = await _pdfFileNavigator.GetRootPageTreeNodeAsync();

        return await DereferenceIndirectObjectAsync(rootPageTreeNode.Id.Reference);
    }

    /// <summary>
    /// Gets the latest trailer which has been written to the file.
    /// </summary>
    public async Task<Trailer?> GetRootTrailerAsync()
    {
        var workingUpdate = GetWorkingIncrementalUpdate();

        return Updates.LastOrDefault(u => u.Written)?.Trailer
            ?? await _pdfFileNavigator.GetRootTrailerAsync();
    }

    /// <summary>
    /// Gets the latest trailer dictionary which has been written to the file.
    /// </summary>
    public async Task<ITrailerDictionary> GetRootTrailerDictionaryAsync()
    {
        var workingUpdate = GetWorkingIncrementalUpdate();

        var lastWrittenUpdate = Updates.LastOrDefault(u => u.Written);

        return lastWrittenUpdate?.TrailerDictionary
            ?? lastWrittenUpdate?.Trailer?.Dictionary
            ?? await _pdfFileNavigator.GetRootTrailerDictionaryAsync();

    }

    public async Task<IndirectObjectId> GetFreeIndexAsync()
    {
        var xrefs = await GetAggregateCrossReferencesAsync();

        IndirectObjectId newObjectId;
        var free = xrefs.FirstOrDefault(x => !x.Value.InUse);
        if (free.Key != 0)
        {
            newObjectId = new IndirectObjectId(free.Key, free.Value.Value2);
        }
        else
        {
            newObjectId = new IndirectObjectId(xrefs.Count + 1, 0);
        }

        return newObjectId;
    }

    public async Task<IndirectObject> AddNewObjectAsync(PdfObject pdfObject)
    {
        if (pdfObject is null) throw new ArgumentNullException(nameof(pdfObject));

        IndirectObjectId newObjectId = await GetFreeIndexAsync();

        var indirectObject = new IndirectObject(newObjectId, pdfObject);

        var latestIncrementalUpdate = GetWorkingIncrementalUpdate();

        latestIncrementalUpdate.NewObjects.Add(indirectObject);

        return indirectObject;
    }

    public void UpdateObject(IndirectObject indirectObject)
    {
        if (indirectObject is null) throw new ArgumentNullException(nameof(indirectObject));

        var latestIncrementalUpdate = GetWorkingIncrementalUpdate();

        latestIncrementalUpdate.UpdatedObjects[indirectObject.Id] = indirectObject;
    }

    public void DeleteObject(IndirectObjectId indirectObjectId)
    {
        if (indirectObjectId is null) throw new ArgumentNullException(nameof(indirectObjectId));

        indirectObjectId.GenerationNumber++;

        var latestIncrementalUpdate = GetWorkingIncrementalUpdate();

        latestIncrementalUpdate.DeletedObjects.Add(indirectObjectId);
    }
}
