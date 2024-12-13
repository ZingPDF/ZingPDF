using System.Text;
using ZingPDF.Logging;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.Objects;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.IncrementalUpdates;

namespace ZingPDF.Parsing;

internal class IndirectObjectDictionary : IIndirectObjectDictionary
{
    private readonly Dictionary<int, CrossReferenceEntry> _xrefEntryCache = [];
    private readonly Dictionary<IndirectObjectId, IndirectObject> _parsedObjectCache = [];

    private readonly Stream _pdfInputStream;
    private readonly List<DocumentVersion> _versions;

    public IndirectObjectDictionary(Stream pdfInputStream, List<DocumentVersion> versions)
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));
        ArgumentNullException.ThrowIfNull(versions, nameof(versions));

        _pdfInputStream = pdfInputStream;
        _versions = versions;
    }

    public int Count => throw new NotImplementedException();

    public async Task<IndirectObject?> GetAsync(IndirectObjectReference key)
    {
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

        // Look for object backwards through versions.
        // Each version will have a table or stream.
        // The table contains parsed xref entries and their locations.
        // The stream needs decoding to get to its entries.

        foreach (var version in _versions)
        {
            // First, process the table if present, caching object locations
            if (version.CrossReferenceTable != null)
            {
                ProcessXrefTable(version.CrossReferenceTable);
            }
            else
            {
                await ProcessXrefStreamAsync(version.CrossReferenceStream!);
            }
        }

        throw new NotImplementedException();
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

            return await Parser.IndirectObjects.ParseAsync(_pdfInputStream, this);
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

        return new IndirectObject(key.Id, await Parser.For(type).ParseAsync(decompressedObjectStream, this));
    }

    public List<IndirectObjectId> GetFreeIds()
    {
        throw new NotImplementedException();
    }

    public async Task<T?> GetAsync<T>(IndirectObjectReference key) where T : class, IPdfObject
    {
        var io = await GetAsync(key);

        return io == null ? default : (T)io.Object;
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