using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Parsing.Parsers.FileStructure.CrossReferences;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Parsing.Parsers.FileStructure;

internal static class DocumentVersionParser
{
    // Parse all xref tables and streams to get all versions of the file
    public static async Task<List<VersionInformation>> ParseDocumentVersionsAsync(Stream pdfInputStream)
    {
        List<VersionInformation> versions = [];

        int? xrefOffset = await GetMainXrefOffsetAsync(pdfInputStream);

        while (xrefOffset != null)
        {
            var version = await ParseDocumentVersionAsync(pdfInputStream, xrefOffset.Value);
            
            xrefOffset = version.TrailerDictionary.Prev;

            versions.Add(version);
        }

        return versions;
    }

    // TODO: move to testable class

    // Given the byte offset of an xref table or stream, parse and produce a DocumentVersion instance
    private static async Task<VersionInformation> ParseDocumentVersionAsync(Stream pdfInputStream, int xrefOffset)
    {
        VersionInformation version;

        pdfInputStream.Position = xrefOffset;

        var type = await TokenTypeIdentifier.TryIdentifyAsync(pdfInputStream);

        if (type == typeof(Keyword))
        {
            var xrefTable = await Parser.XrefTables.ParseAsync(pdfInputStream);

            version = new VersionInformation
            {
                CrossReferenceTable = xrefTable,
                Trailer = await Parser.Trailers.ParseAsync(pdfInputStream),
                IndirectObjects = new IndirectObjectDictionary(pdfInputStream, ProcessXrefTable(xrefTable))
            };
        }
        else if (type == typeof(IndirectObject))
        {
            var xrefStream = await new CrossReferenceStreamParser().ParseAsync(pdfInputStream);

            version = new VersionInformation
            {
                CrossReferenceStream = xrefStream,
                IndirectObjects = new IndirectObjectDictionary(pdfInputStream, await ProcessXrefStreamAsync(xrefStream))
            };
        }
        else
        {
            throw new InvalidOperationException("No xrefs found at offset");
        }

        return version;
    }

    /// <summary>
    /// Searches from the end of the file for the startxref keyword, parses the value and returns it.
    /// </summary>
    private static async Task<int> GetMainXrefOffsetAsync(Stream pdfStream)
    {
        // startxref
        // Byte_offset_of_last_cross-reference_section
        // %%EOF

        var objectFinder = new ObjectFinder();

        // First, find the startxref keyword
        var offset = await objectFinder.FindAsync(pdfStream, Constants.StartXref, forwards: false)
            ?? throw new InvalidOperationException($"{Constants.StartXref} not found.");

        pdfStream.Position = offset;

        _ = await Parser.Keywords.ParseAsync(pdfStream);

        return await Parser.Integers.ParseAsync(pdfStream);
    }

    private static Dictionary<int, CrossReferenceEntry> ProcessXrefTable(CrossReferenceTable xrefTable)
    {
        Dictionary<int, CrossReferenceEntry> xrefs = [];

        foreach (var section in xrefTable.Sections)
        {
            for (var i = 0; i < section.Entries.Count; i++)
            {
                var entry = section.Entries[i];

                if (!xrefs.TryAdd(section.Index.StartIndex + i, entry))
                {
                    throw new InvalidOperationException($"Duplicate xref in table {section.Index.StartIndex + i}:{entry.Value1}:{entry.Value2}");
                }
            }
        }

        return xrefs;
    }

    private static async Task<Dictionary<int, CrossReferenceEntry>> ProcessXrefStreamAsync(StreamObject<CrossReferenceStreamDictionary> xrefStream)
    {
        Dictionary<int, CrossReferenceEntry> xrefs = [];

        // Get the indices for each subsection
        List<CrossReferenceSectionIndex> xrefIndices = [];
        if (xrefStream.Dictionary.Index is null)
        {
            // Index defaults to a start index of zero, and the size for the count.
            xrefIndices.Add(new CrossReferenceSectionIndex(0, xrefStream.Dictionary.Size));
        }
        else
        {
            // Index contains a pair of integers for each subsection
            // representing the start index and count
            for (var i = 0; i < xrefStream.Dictionary.Index.Count(); i += 2)
            {
                xrefIndices.Add(
                    new CrossReferenceSectionIndex(
                        xrefStream.Dictionary.Index.Get<Integer>(i)!,
                        xrefStream.Dictionary.Index.Get<Integer>(i + 1)!
                        )
                    );
            }
        }

        // TODO: think about this. The property uses the ShorthandArrayObject type as it is written as either a Name OR array of Names.
        // However during parsing, the object will be identified as either a Name or ArrayObject. The parsed value will never match the property type of ShorthandArrayObject.
        // For now, we try resolving the property as either type.

        var filters = (xrefStream.Dictionary.Filter?.Value as ArrayObject)?.Cast<Name>()
            ?? [(xrefStream.Dictionary.Filter?.Value as Name)!];

        var decodeParms = (xrefStream.Dictionary.DecodeParms?.Value as ArrayObject)?.Cast<Dictionary>()
            ?? [(xrefStream.Dictionary.DecodeParms?.Value as Dictionary)!];

        var xrefData = await (await xrefStream.Data.UncompressAsync(filters, decodeParms)).ReadToEndAsync();

        var entrySize = xrefStream.Dictionary.W.Sum(x => (x as Integer)!);

        var field1Size = xrefStream.Dictionary.W.Get<Integer>(0)!;
        var field2Size = xrefStream.Dictionary.W.Get<Integer>(1)!;
        var field3Size = xrefStream.Dictionary.W.Get<Integer>(2)!;

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

                xrefs.TryAdd(index.StartIndex + j, new CrossReferenceEntry(field2, (ushort)field3, inUse: entryType != 0, compressed: entryType == 2));
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
