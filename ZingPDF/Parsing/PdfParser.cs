using ZingPDF.Linearization;
using ZingPDF.Logging;
using ZingPDF.Syntax;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Extensions;

namespace ZingPDF.Parsing;

public class PdfParser
{
    public static Task<Pdf> OpenAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

        return OpenAsync(File.Open(filePath, FileMode.Open));
    }

    public static async Task<Pdf> OpenAsync(Stream pdfInputStream)
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));

        if (!pdfInputStream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable", nameof(pdfInputStream));
        }

        var source = await OpenReadOnlyAsync(pdfInputStream);

        return new Pdf(
            pdfInputStream,
            source.DocumentCatalog,
            source.Trailer,
            source.CrossReferenceStream,
            new IncrementalUpdates.IndirectObjectManager(source.IndirectObjects),
            source.LinearizationDictionary
            );
    }

    public static async Task<ReadOnlyPdf> OpenReadOnlyAsync(Stream pdfInputStream)
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));

        if (!pdfInputStream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable", nameof(pdfInputStream));
        }

        // TODO: Check the efficiency of this method, ensure we're traversing the file properly and not parsing things twice.

        // Parse PDF for various core elements

        // 1. Is there a linearization parameter dictionary
        var linearizationDictionary = await GetLinearizationDictionaryAsync(pdfInputStream);

        // PDF is linearized if there is a linearization dictionary, AND
        // the length value (L) is identical to the length of the stream.
        // A mismatch indicates the file has had at least one incremental update applied,
        // and should be considered to not be linearized.
        var isLinearized = linearizationDictionary != null && linearizationDictionary.L == pdfInputStream.Length;

        return isLinearized
            ? await ParseLinearizedAsync(pdfInputStream, linearizationDictionary, pdfInputStream.Position)
            : await ParseNonLinearizedAsync(pdfInputStream);
    }

    private static async Task<ReadOnlyPdf> ParseNonLinearizedAsync(Stream pdfInputStream)
    {
        var offset = await new ObjectFinder().FindAsync(pdfInputStream, Constants.StartXref, forwards: false)
                ?? throw new InvalidOperationException($"{Constants.StartXref} not found.");

        pdfInputStream.Position = offset;
        await pdfInputStream.AdvanceBeyondNextAsync(Constants.StartXref);

        var xrefLocation = await Parser.For<Integer>().ParseAsync(pdfInputStream);

        var trailer = await GetFooterTrailerAsync(pdfInputStream);

        var xrefStream = await GetXrefStreamAsync(pdfInputStream);

        var trailerDictionary = trailer?.Dictionary
            ?? ((StreamObject<IStreamDictionary>)xrefStream?.Object!).Dictionary as ITrailerDictionary
            ?? throw new ParserException("Unable to find trailer dictionary");

        var indirectObjectDictionary = await new CrossReferenceAggregator().AggregateAsync(pdfInputStream, xrefLocation);

        var documentCatalog = await indirectObjectDictionary.GetAsync<DocumentCatalogDictionary>(trailerDictionary.Root)
            ?? throw new ParserException("Unable to find document catalog dictionary");

        return new ReadOnlyPdf(pdfInputStream, documentCatalog!, trailer, xrefStream, indirectObjectDictionary, null);
    }

    private static async Task<ReadOnlyPdf> ParseLinearizedAsync(
        Stream pdfInputStream,
        LinearizationParameterDictionary? linearizationDictionary,
        long xrefLocation
        )
    {
        var indirectObjectDictionary = await new CrossReferenceAggregator().AggregateAsync(pdfInputStream, xrefLocation);

        var xrefStream = await GetXrefStreamAsync(pdfInputStream);

        var trailer = await GetLeadingTrailerAsync(pdfInputStream);

        var trailerDictionary = trailer?.Dictionary
            ?? ((StreamObject<IStreamDictionary>)xrefStream?.Object!).Dictionary as ITrailerDictionary
            ?? throw new ParserException("Unable to find trailer dictionary");

        var documentCatalog = await indirectObjectDictionary.GetAsync<DocumentCatalogDictionary>(trailerDictionary.Root)
            ?? throw new ParserException("Unable to find document catalog dictionary");

        return new ReadOnlyPdf(pdfInputStream, documentCatalog!, trailer, xrefStream, indirectObjectDictionary, linearizationDictionary);
    }

    private static Task<Trailer?> GetFooterTrailerAsync(Stream pdfStream)
        => GetTrailerAsync(pdfStream, false);

    private static Task<Trailer?> GetLeadingTrailerAsync(Stream pdfStream)
        => GetTrailerAsync(pdfStream, true);

    private static async Task<Trailer?> GetTrailerAsync(Stream pdfStream, bool fromTop)
    {
        Logger.Log(LogLevel.Trace, $"Searching for root trailer");

        var xrefObject = await GetXrefObjectAsync(pdfStream);

        if (xrefObject is not Keyword k || k != Constants.Xref)
        {
            return null;
        }

        var objectFinder = new ObjectFinder();

        var trailerOffset = await objectFinder.FindAsync(pdfStream, Constants.Trailer, forwards: fromTop);

        if (trailerOffset is null)
        {
            return null;
        }

        pdfStream.Position = trailerOffset.Value;

        var trailer = await Parser.For<Trailer>().ParseAsync(pdfStream);

        return trailer;
    }

    private static async Task<int> GetXrefOffsetAsync(Stream pdfStream)
    {
        // trailer
        // <<key1 value1
        // key2 value2
        // …
        // keyn valuen
        // >>
        // startxref
        // Byte_offset_of_last_cross-reference_section
        // %%EOF

        var objectFinder = new ObjectFinder();

        // First, find the startxref keyword
        var offset = await objectFinder.FindAsync(pdfStream, Constants.StartXref, forwards: false)
            ?? throw new InvalidOperationException($"{Constants.StartXref} not found.");

        pdfStream.Position = offset;

        _ = await Parser.For<Keyword>().ParseAsync(pdfStream);
        return await Parser.For<Integer>().ParseAsync(pdfStream);
    }

    private static async Task<IndirectObject?> GetXrefStreamAsync(Stream pdfStream)
    {
        Logger.Log(LogLevel.Trace, $"Searching for root trailer dictionary");

        var xrefObject = await GetXrefObjectAsync(pdfStream);

        if (xrefObject is IndirectObject io
            && io.Object is StreamObject<IStreamDictionary> so
            && so.Dictionary is CrossReferenceStreamDictionary)
        {
            Logger.Log(LogLevel.Trace, $"Found cross reference stream dictionary");

            return io;
        }

        return null;
    }

    private static async Task<IPdfObject> GetXrefObjectAsync(Stream pdfStream)
    {
        var xrefOffset = await GetXrefOffsetAsync(pdfStream);

        pdfStream.Position = xrefOffset;

        var type = await TokenTypeIdentifier.TryIdentifyAsync(pdfStream);

        var item = await Parser.For(type).ParseAsync(pdfStream);

        return item;
    }

    private static async Task<LinearizationParameterDictionary?> GetLinearizationDictionaryAsync(Stream pdfStream)
    {
        Logger.Log(LogLevel.Trace, $"Searching for linearisation dictionary");

        pdfStream.Position = 0;

        var limit = Math.Min(1024, pdfStream.Length);

        while (pdfStream.Position < limit)
        {
            var type = await TokenTypeIdentifier.TryIdentifyAsync(pdfStream);
            if (type is null)
            {
                // TODO: is this a valid scenario?
                break;
            }

            var item = await Parser.For(type).ParseAsync(pdfStream);

            if (item is IndirectObject o && o.Object is LinearizationParameterDictionary dict)
            {
                Logger.Log(LogLevel.Trace, $"Found linearisation dictionary");

                return dict;
            }
        }

        Logger.Log(LogLevel.Trace, $"No linearisation dictionary found");

        return null;
    }
}
