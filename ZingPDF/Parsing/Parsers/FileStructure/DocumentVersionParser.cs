using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Parsing.Parsers.FileStructure;

internal class DocumentVersionParser
{
    // Parse all xref tables and streams to get all versions of the file
    public static async Task<List<DocumentVersion>> ParseDocumentVersionsAsync(Stream pdfInputStream)
    {
        List<DocumentVersion> versions = [];

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

    // Give the byte offset of an xref table or stream, parse and produce a DocumentVersion instance
    private static async Task<DocumentVersion> ParseDocumentVersionAsync(Stream pdfInputStream, int xrefOffset)
    {
        DocumentVersion version;

        pdfInputStream.Position = xrefOffset;

        var type = await TokenTypeIdentifier.TryIdentifyAsync(pdfInputStream);

        if (type == typeof(Keyword))
        {
            version = new DocumentVersion
            {
                CrossReferenceTable = await Parser.XrefTables.ParseAsync(pdfInputStream),
                Trailer = await Parser.Trailers.ParseAsync(pdfInputStream)
            };
        }
        else if (type == typeof(IndirectObject))
        {
            var xrefStream = await Parser.For<StreamObject<IStreamDictionary>>().ParseAsync(pdfInputStream);

            version = new DocumentVersion
            {
                CrossReferenceStream = xrefStream
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
}
