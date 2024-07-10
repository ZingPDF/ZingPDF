using System.Text;
using ZingPDF.Logging;
using ZingPDF.ObjectModel.FileStructure.CrossReferences;
using ZingPDF.ObjectModel.FileStructure.ObjectStreams;
using ZingPDF.ObjectModel.Objects.IndirectObjects;
using ZingPDF.ObjectModel.Objects.Streams;
using ZingPDF.Parsing.Parsers;

namespace ZingPDF.Parsing;

/// <summary>
/// Read only <see cref="IIndirectObjectDictionary"/> containing all of the PDFs indirect objects.<para></para>
/// </summary>
public class ReadOnlyIndirectObjectDictionary(Stream stream, Dictionary<int, CrossReferenceEntry> xrefs) : IIndirectObjectDictionary
{
    private readonly Dictionary<IndirectObjectId, IndirectObject> _indirectObjectCache = [];
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));

    public int Count => xrefs.Count;

    /// <summary>
    /// Returns the latest Indirect Object matching the given reference.
    /// </summary>
    public async Task<IndirectObject?> GetAsync(IndirectObjectReference key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var indirectObject = await GetOrAddAsync(key, async () =>
        {
            if (!xrefs.TryGetValue(key.Id.Index, out CrossReferenceEntry? xref))
            {
                return null;
            }

            var indirectObjectParser = Parser.For<IndirectObject>();

            if (xref.Compressed)
            {
                Logger.Log(LogLevel.Trace, $"{key} is compressed within object stream {xref.Value1}");

                // TODO: must support the `Extends` property

                var objStreamIndirectObject = await GetAsync(new IndirectObjectReference(new IndirectObjectId((int)xref.Value1, 0)))
                    ?? throw new InvalidOperationException($"Error attempting to parse {key}. Unable to find parent object stream {xref.Value1}");

                var objectStream = objStreamIndirectObject.Get<IStreamObject<IStreamDictionary>>()!;
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

                return new IndirectObject(key.Id, await Parser.For(type).ParseAsync(decompressedObjectStream));
            }
            else
            {
                _stream.Position = xref.Value1;

                return await indirectObjectParser.ParseAsync(_stream);
            }

        });

        return indirectObject;
    }

    /// <summary>
    /// When you know the indirect object contains a single object of a specific type, 
    /// this method provides strongly typed access to it.
    /// </summary>
    public async Task<T?> GetAsync<T>(IndirectObjectReference key)
    {
        var io = await GetAsync(key);

        return io == null ? default : (T)io.Children.First();
    }

    public List<IndirectObjectId> GetFreeIds()
    {
        return xrefs
            .Where(x => !x.Value.InUse && x.Value.Value1 != 0)
            .Select(x => new IndirectObjectId((int)x.Value.Value1, x.Value.Value2))
            .ToList();
    }

    private async Task<IndirectObject?> GetOrAddAsync(
        IndirectObjectReference reference,
        Func<Task<IndirectObject?>> ioRetreiver
        )
    {
        if (_indirectObjectCache.TryGetValue(reference.Id, out IndirectObject? indirectObject))
        {
            Logger.Log(LogLevel.Trace, $"{reference} returned from cache");

            return indirectObject;
        }

        Logger.Log(LogLevel.Trace, $"Cache miss: {reference}");

        indirectObject = await ioRetreiver();

        if (indirectObject == null)
        {
            return null;
        }

        _indirectObjectCache.TryAdd(reference.Id, indirectObject);

        return indirectObject;
    }
}
