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
        
        return await ParseNonLinearizedAsync(pdfInputStream);

        //// The PDF is linearized if there is a linearization dictionary, AND
        //// the length value (L) is identical to the length of the stream.
        //// A mismatch indicates the file has had at least one incremental update applied,
        //// and should be considered to not be linearized, at which point we can search from the bottom of the file.

        //var linearizationParser = new LinearizationParser();

        //var linearizationDictionary = await linearizationParser.GetLinearizationDictionaryAsync(pdfInputStream);

        //return linearizationDictionary != null
        //    ? await ParseLinearizedAsync(pdfInputStream, linearizationDictionary, pdfInputStream.Position)
        //    : await ParseNonLinearizedAsync(pdfInputStream);
    }

    private static async Task<ReadOnlyPdf> ParseNonLinearizedAsync(Stream pdfInputStream)
    {
        var offset = await new ObjectFinder().FindAsync(pdfInputStream, Constants.StartXref, forwards: false)
                ?? throw new InvalidOperationException($"{Constants.StartXref} not found.");

        pdfInputStream.Position = offset;
        await pdfInputStream.AdvanceBeyondNextAsync(Constants.StartXref);

        var xrefLocation = await Parser.Integers.ParseAsync(pdfInputStream, HoneyTrapIndirectObjectDictionary.Instance);

        var indirectObjectDictionary = await new CrossReferenceAggregator().AggregateAsync(pdfInputStream, xrefLocation);

        var trailer = await GetFooterTrailerAsync(pdfInputStream, indirectObjectDictionary);

        var xrefStream = await GetXrefStreamAsync(pdfInputStream, indirectObjectDictionary);

        var trailerDictionary = trailer?.Dictionary
            ?? ((StreamObject<IStreamDictionary>)xrefStream?.Object!).Dictionary as ITrailerDictionary
            ?? throw new ParserException("Unable to find trailer dictionary");

        var documentCatalog = await indirectObjectDictionary.GetAsync<DocumentCatalogDictionary>(trailerDictionary.Root)
            ?? throw new ParserException("Unable to find document catalog dictionary");

        return new ReadOnlyPdf(pdfInputStream, documentCatalog!, trailer, xrefStream, indirectObjectDictionary, null);
    }

    //private static async Task<ReadOnlyPdf> ParseLinearizedAsync(
    //    Stream pdfInputStream,
    //    LinearizationParameterDictionary? linearizationDictionary,
    //    long xrefLocation
    //    )
    //{
    //    var indirectObjectDictionary = await new CrossReferenceAggregator().AggregateAsync(pdfInputStream, xrefLocation);

    //    var xrefStream = await GetXrefStreamAsync(pdfInputStream, indirectObjectDictionary);

    //    var trailer = await GetLeadingTrailerAsync(pdfInputStream, indirectObjectDictionary);

    //    var trailerDictionary = trailer?.Dictionary
    //        ?? ((StreamObject<IStreamDictionary>)xrefStream?.Object!).Dictionary as ITrailerDictionary
    //        ?? throw new ParserException("Unable to find trailer dictionary");

    //    var documentCatalog = await indirectObjectDictionary.GetAsync<DocumentCatalogDictionary>(trailerDictionary.Root)
    //        ?? throw new ParserException("Unable to find document catalog dictionary");

    //    return new ReadOnlyPdf(pdfInputStream, documentCatalog!, trailer, xrefStream, indirectObjectDictionary, linearizationDictionary);
    //}

    private static Task<Trailer?> GetFooterTrailerAsync(Stream pdfStream, IIndirectObjectDictionary indirectObjectDictionary)
        => GetTrailerAsync(pdfStream, false, indirectObjectDictionary);

    private static Task<Trailer?> GetLeadingTrailerAsync(Stream pdfStream, IIndirectObjectDictionary indirectObjectDictionary)
        => GetTrailerAsync(pdfStream, true, indirectObjectDictionary);

    private static async Task<Trailer?> GetTrailerAsync(Stream pdfStream, bool fromTop, IIndirectObjectDictionary indirectObjectDictionary)
    {
        Logger.Log(LogLevel.Trace, $"Searching for root trailer");

        var xrefObject = await GetXrefObjectAsync(pdfStream, indirectObjectDictionary);

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

        var trailer = await Parser.Trailers.ParseAsync(pdfStream, indirectObjectDictionary);

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

        _ = await Parser.Keywords.ParseAsync(pdfStream, HoneyTrapIndirectObjectDictionary.Instance);
        return await Parser.Integers.ParseAsync(pdfStream, HoneyTrapIndirectObjectDictionary.Instance);
    }

    private static async Task<IndirectObject?> GetXrefStreamAsync(Stream pdfStream, IIndirectObjectDictionary indirectObjectDictionary)
    {
        Logger.Log(LogLevel.Trace, $"Searching for root trailer dictionary");

        var xrefObject = await GetXrefObjectAsync(pdfStream, indirectObjectDictionary);

        if (xrefObject is IndirectObject io
            && io.Object is StreamObject<IStreamDictionary> so
            && so.Dictionary is CrossReferenceStreamDictionary)
        {
            Logger.Log(LogLevel.Trace, $"Found cross reference stream dictionary");

            return io;
        }

        return null;
    }

    private static async Task<IPdfObject> GetXrefObjectAsync(Stream pdfStream, IIndirectObjectDictionary indirectObjectDictionary)
    {
        var xrefOffset = await GetXrefOffsetAsync(pdfStream);

        pdfStream.Position = xrefOffset;

        var type = await TokenTypeIdentifier.TryIdentifyAsync(pdfStream);

        var item = await Parser.For(type).ParseAsync(pdfStream, indirectObjectDictionary);

        return item;
    }
}

internal class HoneyTrapIndirectObjectDictionary : IIndirectObjectDictionary
{
    private static readonly IIndirectObjectDictionary _instance = new HoneyTrapIndirectObjectDictionary();

    private const string _error = "If you're seeing this, ZingPDF is broken.";

    public int Count => throw new InvalidOperationException(_error);
    public Task<IndirectObject?> GetAsync(IndirectObjectReference key) => throw new InvalidOperationException(_error);
    public List<IndirectObjectId> GetFreeIds() => throw new InvalidOperationException(_error);
    Task<T?> IIndirectObjectDictionary.GetAsync<T>(IndirectObjectReference key) where T : class
        => throw new InvalidOperationException(_error);

    public static IIndirectObjectDictionary Instance => _instance;
}