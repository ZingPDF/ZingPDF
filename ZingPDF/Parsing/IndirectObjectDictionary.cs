using System.Text;
using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Logging;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing;

internal class IndirectObjectDictionary : IIndirectObjectDictionary
{
    private readonly Dictionary<int, CrossReferenceEntry> _xrefEntryCache = [];
    private readonly Dictionary<IndirectObjectId, IndirectObject> _parsedObjectCache = [];

    private readonly Stream _pdfInputStream;
    private readonly List<DocumentVersion> _versions;

    private readonly List<IndirectObject> _newObjects = [];
    private readonly Dictionary<IndirectObjectId, IndirectObject> _updatedObjects = [];
    private readonly List<IndirectObjectId> _deletedObjects = [];

    private readonly Queue<IndirectObjectId> _freeIds;

    public IndirectObjectDictionary(Stream pdfInputStream, List<DocumentVersion> versions)
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));
        ArgumentNullException.ThrowIfNull(versions, nameof(versions));

        _pdfInputStream = pdfInputStream;
        _versions = versions;

        _freeIds = new Queue<IndirectObjectId>(GetFreeIds());
    }

    public int Count
    {
        get
        {
            if (_xrefEntryCache.Count == 0)
            {
                throw new InvalidOperationException("IndexObjectsAsync method must be called before attempting to access the Count property.");
            }

            return _xrefEntryCache.Count(x => x.Value.InUse) + _newObjects.Count - _deletedObjects.Count;
        }
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

    public async Task<IndirectObject?> GetAsync(IndirectObjectReference key)
    {
        if (_xrefEntryCache.Count == 0)
        {
            throw new InvalidOperationException("IndexObjectsAsync method must be called before attempting to dereference objects.");
        }

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

        // Check the local object cache first.
        if (_parsedObjectCache.TryGetValue(key.Id, out var cachedObj))
        {
            return cachedObj;
        }

        // If not fully parsed, we may still have saved the location of the entry.
        if (_xrefEntryCache.TryGetValue(key.Id.Index, out var cachedXrefEntry))
        {
            IndirectObject obj = await DereferenceObjectAsync(key, cachedXrefEntry);

            _parsedObjectCache.Add(key.Id, obj);

            return obj;
        }

        return null;
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

    public void Update(IndirectObject indirectObject)
    {
        ArgumentNullException.ThrowIfNull(indirectObject);

        _updatedObjects[indirectObject.Id] = indirectObject;
    }

    public void Delete(IndirectObjectId indirectObjectId)
    {
        ArgumentNullException.ThrowIfNull(indirectObjectId);

        indirectObjectId.GenerationNumber++;

        _deletedObjects.Add(indirectObjectId);
    }

    public async Task IndexObjectsAsync()
    {
        // Versions are assumed to be orders with the most recent first.
        // This should happen naturally during parsing.
        // Each method below uses TryAdd to cache references, so more recent versions take precedence.
        foreach (var version in _versions)
        {
            if (version.CrossReferenceTable != null)
            {
                ProcessXrefTable(version.CrossReferenceTable);
            }
            else
            {
                await ProcessXrefStreamAsync(version.CrossReferenceStream!);
            }
        }
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

        return new IncrementalUpdate(
            new DocumentVersion { Trailer = trailer, CrossReferenceTable = xrefTable },
            NewOrUpdatedObjects
            );
    }

    private List<IndirectObjectId> GetFreeIds()
    {
        // TODO: it might be more efficient to traverse the linked list of free entries here.

        return _xrefEntryCache.Where(x => !x.Value.InUse)
            .Select(x => new IndirectObjectId(x.Key, x.Value.Value2))
            .ToList();
    }

    private IndirectObjectId GetNextFreeId()
    {
        if (_freeIds.TryDequeue(out var id))
        {
            return id;
        }

        return new IndirectObjectId(Count + 1, 0);
    }

    private void ProcessXrefTable(CrossReferenceTable xrefTable)
    {
        foreach (var section in xrefTable.Sections)
        {
            for (var i = 0; i < section.Entries.Count; i++)
            {
                var entry = section.Entries[i];

                if (!_xrefEntryCache.TryAdd(section.Index.StartIndex + i, entry))
                {
                    Console.WriteLine($"Entry already present in xrefs {section.Index.StartIndex + i}:{entry.Value1}:{entry.Value2}");
                }
            }
        }
    }

    private async Task ProcessXrefStreamAsync(StreamObject<IStreamDictionary> xrefStream)
    {
        var xrefStreamDictionary = (xrefStream.Dictionary as CrossReferenceStreamDictionary)!;

        // Get the indices for each subsection
        List<CrossReferenceSectionIndex> xrefIndices = [];
        if (xrefStreamDictionary.Index is null)
        {
            // Index defaults to a start index of zero, and the size for the count.
            xrefIndices.Add(new CrossReferenceSectionIndex(0, xrefStreamDictionary.Size));
        }
        else
        {
            // Index contains a pair of integers for each subsection
            // representing the start index and count
            for (var i = 0; i < xrefStreamDictionary.Index.Count(); i += 2)
            {
                xrefIndices.Add(
                    new CrossReferenceSectionIndex(
                        xrefStreamDictionary.Index.Get<Integer>(i)!,
                        xrefStreamDictionary.Index.Get<Integer>(i + 1)!
                        )
                    );
            }
        }

        var xrefData = await (await xrefStream.Data.GetDecompressedDataAsync()).ReadToEndAsync();
        var entrySize = xrefStreamDictionary.W.Sum(x => (x as Integer)!);

        var field1Size = xrefStreamDictionary.W.Get<Integer>(0)!;
        var field2Size = xrefStreamDictionary.W.Get<Integer>(1)!;
        var field3Size = xrefStreamDictionary.W.Get<Integer>(2)!;

        for (int i = 0; i < xrefIndices.Count; i++)
        {
            CrossReferenceSectionIndex? index = xrefIndices[i];

            var sectionOffset = index.StartIndex * entrySize;

            for (var j = 0; j < index.Count; j++)
            {
                var entryOffset = (i + j) * entrySize;
                var entryData = xrefData[entryOffset..(entryOffset + entrySize)];

                // Default entry type is 1 ('in use' object)
                var entryType = (byte)1;

                if (field1Size != 0)
                {
                    entryType = entryData[0];
                }

                int field2 = ExtractField(entryData, field1Size, field2Size);
                int field3 = ExtractField(entryData, field1Size + field2Size, field3Size);

                _xrefEntryCache.TryAdd(index.StartIndex + j, new CrossReferenceEntry(field2, (ushort)field3, inUse: entryType != 0, compressed: entryType == 2));
            }
        }
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
        Stream decompressedObjectStream = await objectStream.Data.GetDecompressedDataAsync();
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

    /// <summary>
    /// Function to extract multi-byte fields
    /// </summary>
    /// <remarks>
    /// The field is stored in big-endian order, where the most significant byte is at the lowest memory address.
    /// The function iterates over each byte in the field, masks out the lower 8 bits,
    /// and left-shifts the value by an appropriate amount based on its position in the field.
    /// The result is accumulated to reconstruct the final field value.
    /// </remarks>
    private static int ExtractField(byte[] data, int startIndex, int fieldSize)
    {
        int fieldValue = 0;

        for (var i = 0; i < fieldSize; i++)
        {
            fieldValue += (data[i + startIndex] & 0x00FF) << (fieldSize - i - 1) * 8;
        }

        return fieldValue;
    }
}