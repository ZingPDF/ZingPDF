using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Parsing.Parsers.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Parsing;

internal class CrossReferenceAggregator
{
    public async Task<ReadOnlyIndirectObjectDictionary> AggregateAsync(
        Stream pdfInputStream,
        long xrefLocation
        )
    {
        Logger.Log(LogLevel.Trace, $"Aggregating cross references");

        pdfInputStream.Position = xrefLocation;
         
        Dictionary<int, CrossReferenceEntry> xrefs = [];

        await ParseCrossReferencesAsync(pdfInputStream, xrefs);

        return new ReadOnlyIndirectObjectDictionary(pdfInputStream, xrefs);
    }

    private static async Task ParseCrossReferencesAsync(Stream pdfStream, Dictionary<int, CrossReferenceEntry> xrefs)
    {
        // The next object will either be an xref table, or stream.
        var type = await TokenTypeIdentifier.TryIdentifyAsync(pdfStream)
            ?? throw new InvalidOperationException("Unable to find cross reference table or stream. PDF may be corrupt.");

        var item = await Parser.For(type).ParseAsync(pdfStream, HoneyTrapIndirectObjectDictionary.Instance);

        if (item is IndirectObject io
            && io.Object is StreamObject<IStreamDictionary> streamObject
            && streamObject.Dictionary is CrossReferenceStreamDictionary)
        {
            await ParseCrossReferenceStreamAsync(pdfStream, streamObject, xrefs);
        }
        else if (item is Keyword k && k == Constants.Xref)
        {
            await ParseCrossReferenceTableAsync(pdfStream, xrefs);
        }
        else
        {
            throw new InvalidOperationException("Unable to find PDF cross references.");
        }
    }

    private static async Task ParseCrossReferenceStreamAsync(Stream pdfStream, StreamObject<IStreamDictionary> crossReferenceStream, Dictionary<int, CrossReferenceEntry> xrefs)
    {
        var xrefStreamDictionary = (crossReferenceStream.Dictionary as CrossReferenceStreamDictionary)!;

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

        var xrefData = await (await crossReferenceStream.Data.GetDecompressedDataAsync()).ReadToEndAsync();
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

                xrefs.TryAdd(index.StartIndex + j, new CrossReferenceEntry(field2, (ushort)field3, inUse: entryType != 0, compressed: entryType == 2));
            }
        }

        if (xrefStreamDictionary.Prev is not null)
        {
            pdfStream.Position = xrefStreamDictionary.Prev;

            await ParseCrossReferencesAsync(pdfStream, xrefs);
        }
    }

    private static async Task ParseCrossReferenceTableAsync(Stream pdfStream, Dictionary<int, CrossReferenceEntry> xrefs)
    {
        var xrefTable = await new CrossReferenceTableParser().ParseAsync(pdfStream, HoneyTrapIndirectObjectDictionary.Instance);

        foreach (var section in xrefTable.Sections)
        {
            for (var i = 0; i < section.Entries.Count; i++)
            {
                var entry = section.Entries[i];

                if (!xrefs.TryAdd(section.Index.StartIndex + i, entry))
                {
                    Console.WriteLine($"Entry already present in xrefs {section.Index.StartIndex + i}:{entry.Value1}:{entry.Value2}");
                }
            }
        }

        var trailer = await Parser.Trailers.ParseAsync(pdfStream, HoneyTrapIndirectObjectDictionary.Instance);

        if (trailer.Dictionary.Prev is not null)
        {
            pdfStream.Position = trailer.Dictionary.Prev;

            await ParseCrossReferencesAsync(pdfStream, xrefs);
        }
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
