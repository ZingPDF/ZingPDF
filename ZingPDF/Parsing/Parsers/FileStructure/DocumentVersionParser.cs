using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using System.Text;

namespace ZingPDF.Parsing.Parsers.FileStructure;

internal class DocumentVersionParser : IDocumentVersionParser
{
    private static readonly ObjectContext _objectContext = ObjectContext.WithOrigin(ObjectOrigin.DocumentVersionParser);

    private readonly IParser<Keyword> _keywordParser;
    private readonly IParser<Number> _numberParser;
    private readonly IParser<CrossReferenceTable> _crossReferenceTableParser;
    private readonly IParser<Trailer> _trailerParser;
    private readonly IParser<StreamObject<CrossReferenceStreamDictionary>> _crossReferenceStreamParser;

    public DocumentVersionParser(
        IParser<Keyword> keywordParser,
        IParser<Number> numberParser,
        IParser<CrossReferenceTable> crossReferenceTableParser,
        IParser<Trailer> trailerParser,
        IParser<StreamObject<CrossReferenceStreamDictionary>> crossReferenceStreamParser)
    {
        _keywordParser = keywordParser;
        _numberParser = numberParser;
        _crossReferenceTableParser = crossReferenceTableParser;
        _trailerParser = trailerParser;
        _crossReferenceStreamParser = crossReferenceStreamParser;
    }

    // Parse all xref tables and streams to get all versions of the file
    public async Task<List<VersionInformation>> ParseAsync(Stream pdfInputStream)
    {
        using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("DocumentVersionParser.ParseAsync");
        List<VersionInformation> versions = [];

        var latestVersion = await ParseLatestAsync(pdfInputStream);
        versions.Add(latestVersion);

        int? xrefOffset = latestVersion.TrailerDictionary.Prev;

        while (xrefOffset != null)
        {
            var version = await ParseAtAsync(pdfInputStream, xrefOffset.Value);

            xrefOffset = version.TrailerDictionary.Prev;

            versions.Add(version);
        }

        return versions;
    }

    public async Task<VersionInformation> ParseLatestAsync(Stream pdfInputStream)
    {
        using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("DocumentVersionParser.ParseLatestAsync");
        int xrefOffset = await GetMainXrefOffsetAsync(pdfInputStream);
        return await ParseAtAsync(pdfInputStream, xrefOffset);
    }

    public async Task<VersionInformation> ParseAtAsync(Stream pdfInputStream, int xrefOffset)
    {
        return await ParseDocumentVersionAsync(pdfInputStream, xrefOffset);
    }

    // TODO: move to testable class

    // Given the byte offset of an xref table or stream, parse and produce a DocumentVersion instance
    private async Task<VersionInformation> ParseDocumentVersionAsync(Stream pdfInputStream, int xrefOffset)
    {
        using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("DocumentVersionParser.ParseDocumentVersionAsync");
        VersionInformation version;

        pdfInputStream.Position = xrefOffset;

        int marker = PeekNextNonWhitespaceByte(pdfInputStream);

        if (marker == 'x')
        {
            var xrefTable = await _crossReferenceTableParser.ParseAsync(pdfInputStream, _objectContext);

            version = new VersionInformation
            {
                CrossReferenceTable = xrefTable,
                Trailer = await _trailerParser.ParseAsync(pdfInputStream, _objectContext),
                IndirectObjects = ProcessXrefTable(xrefTable)
            };
        }
        else if (marker >= '0' && marker <= '9')
        {
            version = await ParseCrossReferenceStreamVersionAsync(pdfInputStream, xrefOffset)
                ?? await ParseNearbyCrossReferenceTableAsync(pdfInputStream, xrefOffset)
                ?? throw new InvalidOperationException($"No valid xref stream or nearby xref table found at offset {xrefOffset}.");
        }
        else
        {
            throw new InvalidOperationException("No xrefs found at offset");
        }

        return version;
    }

    private async Task<VersionInformation?> ParseCrossReferenceStreamVersionAsync(Stream pdfInputStream, int xrefOffset)
    {
        long originalPosition = pdfInputStream.Position;

        try
        {
            pdfInputStream.Position = xrefOffset;

            // Parse object number then move stream back to start of object.
            // The xref stream parser will record the byte offset which should be at the start of the indirect object.
            var id = await _numberParser.ParseAsync(pdfInputStream, _objectContext);
            var genNumber = await _numberParser.ParseAsync(pdfInputStream, _objectContext);
            var objKeyword = await _keywordParser.ParseAsync(pdfInputStream, _objectContext);

            if (objKeyword.Value != Constants.ObjStart)
            {
                return null;
            }

            pdfInputStream.Position = xrefOffset;

            var xrefStream = await _crossReferenceStreamParser.ParseAsync(pdfInputStream, _objectContext);

            return new VersionInformation
            {
                CrossReferenceStream = xrefStream,
                IndirectObjects = await ProcessXrefStreamAsync(xrefStream, new IndirectObjectId(id, genNumber), xrefOffset)
            };
        }
        catch (FormatException)
        {
            return null;
        }
        finally
        {
            pdfInputStream.Position = originalPosition;
        }
    }

    private async Task<VersionInformation?> ParseNearbyCrossReferenceTableAsync(Stream pdfInputStream, int xrefOffset)
    {
        int? nearbyOffset = FindNearbyCrossReferenceTableOffset(pdfInputStream, xrefOffset);
        if (nearbyOffset is null)
        {
            return null;
        }

        long originalPosition = pdfInputStream.Position;

        try
        {
            pdfInputStream.Position = nearbyOffset.Value;
            var xrefTable = await _crossReferenceTableParser.ParseAsync(pdfInputStream, _objectContext);

            return new VersionInformation
            {
                CrossReferenceTable = xrefTable,
                Trailer = await _trailerParser.ParseAsync(pdfInputStream, _objectContext),
                IndirectObjects = ProcessXrefTable(xrefTable)
            };
        }
        finally
        {
            pdfInputStream.Position = originalPosition;
        }
    }

    /// <summary>
    /// Searches from the end of the file for the startxref keyword, parses the value and returns it.
    /// </summary>
    private async Task<int> GetMainXrefOffsetAsync(Stream pdfStream)
    {
        using var trace = ZingPDF.Diagnostics.PerformanceTrace.Measure("DocumentVersionParser.GetMainXrefOffsetAsync");
        // startxref
        // Byte_offset_of_last_cross-reference_section
        // %%EOF

        var objectFinder = new ObjectFinder();

        // First, find the startxref keyword
        var offset = await objectFinder.FindAsync(pdfStream, Constants.StartXref, forwards: false)
            ?? throw new InvalidOperationException($"{Constants.StartXref} not found.");

        pdfStream.Position = offset;

        _ = await _keywordParser.ParseAsync(pdfStream, _objectContext);

        return await _numberParser.ParseAsync(pdfStream, _objectContext);
    }

    private static int PeekNextNonWhitespaceByte(Stream stream)
    {
        long originalPosition = stream.Position;

        while (stream.Position < stream.Length)
        {
            int next = stream.ReadByte();
            if (next < 0)
            {
                break;
            }

            if (!char.IsWhiteSpace((char)next))
            {
                stream.Position = originalPosition;
                return next;
            }
        }

        stream.Position = originalPosition;
        return -1;
    }

    private static int? FindNearbyCrossReferenceTableOffset(Stream stream, int xrefOffset)
    {
        const int searchBack = 32;
        const int searchForward = 8;

        long originalPosition = stream.Position;

        try
        {
            int windowStart = Math.Max(0, xrefOffset - searchBack);
            int windowEnd = (int)Math.Min(stream.Length, xrefOffset + searchForward);
            int length = windowEnd - windowStart;
            if (length <= 0)
            {
                return null;
            }

            byte[] buffer = new byte[length];
            stream.Position = windowStart;
            int read = stream.Read(buffer, 0, length);
            if (read < Constants.Xref.Length)
            {
                return null;
            }

            byte[] xrefBytes = Encoding.ASCII.GetBytes(Constants.Xref);

            for (int i = 0; i <= read - xrefBytes.Length; i++)
            {
                if (!buffer.AsSpan(i, xrefBytes.Length).SequenceEqual(xrefBytes))
                {
                    continue;
                }

                bool validPrefix = i == 0 || char.IsWhiteSpace((char)buffer[i - 1]);
                int suffixIndex = i + xrefBytes.Length;
                bool validSuffix = suffixIndex >= read || char.IsWhiteSpace((char)buffer[suffixIndex]);

                if (validPrefix && validSuffix)
                {
                    return windowStart + i;
                }
            }

            return null;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    private static Dictionary<IndirectObjectId, CrossReferenceEntry> ProcessXrefTable(CrossReferenceTable xrefTable)
    {
        Dictionary<IndirectObjectId, CrossReferenceEntry> xrefs = [];

        foreach (var section in xrefTable.Sections)
        {
            for (var i = 0; i < section.Entries.Count; i++)
            {
                var entry = section.Entries[i];
                var key = new IndirectObjectId(section.Index.StartIndex + i, entry.Value2);

                if (!xrefs.TryAdd(key, entry))
                {
                    throw new InvalidOperationException($"Duplicate xref in table {section.Index.StartIndex + i}:{entry.Value1}:{entry.Value2}");
                }
            }
        }

        return xrefs;
    }

    private async Task<Dictionary<IndirectObjectId, CrossReferenceEntry>> ProcessXrefStreamAsync(
        StreamObject<CrossReferenceStreamDictionary> xrefStream,
        IndirectObjectId xrefStreamObjectId,
        long xrefStreamByteOffset
        )
    {
        Dictionary<IndirectObjectId, CrossReferenceEntry> xrefs = [];

        // Get the indices for each subsection
        List<CrossReferenceSectionIndex> xrefIndices = [];
        if (xrefStream.Dictionary.Index is null)
        {
            // Index defaults to a start index of zero, and the size for the count.
            xrefIndices.Add(new CrossReferenceSectionIndex(0, xrefStream.Dictionary.Size, _objectContext));
        }
        else
        {
            // Index contains a pair of integers for each subsection
            // representing the start index and count
            for (var i = 0; i < xrefStream.Dictionary.Index.Count(); i += 2)
            {
                xrefIndices.Add(
                    new CrossReferenceSectionIndex(
                        xrefStream.Dictionary.Index.Get<Number>(i)!,
                        xrefStream.Dictionary.Index.Get<Number>(i + 1)!,
                        _objectContext
                        )
                    );
            }
        }

        var xrefData = await (await xrefStream.GetDecompressedDataAsync()).ReadToEndAsync();

        var entrySize = xrefStream.Dictionary.W.Sum(x => (x as Number)!);

        var field1Size = xrefStream.Dictionary.W.Get<Number>(0)!;
        var field2Size = xrefStream.Dictionary.W.Get<Number>(1)!;
        var field3Size = xrefStream.Dictionary.W.Get<Number>(2)!;

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

                var isCompressed = entryType == 2;
                var generationNumber = (ushort)(isCompressed ? 0 : field3);

                var key = new IndirectObjectId(index.StartIndex + j, generationNumber);

                xrefs.TryAdd(key, new CrossReferenceEntry(
                    field2,
                    (ushort)field3,
                    inUse: entryType != 0,
                    compressed: isCompressed,
                    _objectContext
                    ));
            }
        }

        // Some writers fail to include the xref stream as a reference within itself, as required by the spec.
        // Since this is common, we check for this case and repair.
        if (!xrefs.ContainsKey(xrefStreamObjectId))
        {
            xrefs.Add(
                xrefStreamObjectId,
                new CrossReferenceEntry(
                    xrefStreamByteOffset,
                    xrefStreamObjectId.GenerationNumber,
                    inUse: true,
                    compressed: false,
                    _objectContext
                    )
                );

            if (xrefStream.Dictionary.Size != xrefs.Count)
            {
                xrefStream.Dictionary.SetSize(xrefs.Count);
            }
        }

        return xrefs;
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
