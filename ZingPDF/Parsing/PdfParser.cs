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

namespace ZingPDF.Parsing;

public class PdfParser
{
    public static async Task<Pdf> OpenAsync(Stream pdfInputStream)
    {
        if (!pdfInputStream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable", nameof(pdfInputStream));
        }

        return new Pdf(await OpenReadOnlyAsync(pdfInputStream));
    }

    public static async Task<ReadOnlyPdf> OpenReadOnlyAsync(Stream pdfInputStream)
    {
        if (!pdfInputStream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable", nameof(pdfInputStream));
        }

        // Parse PDF for various core elements

        // 1. Is there a linearization parameter dictionary
        var linearizationDictionary = await GetLinearizationDictionaryAsync(pdfInputStream);

        // 2. Aggregate cross references to get the latest versions of all indirect objects
        var indirectObjectDictionary = await new CrossReferenceAggregator().AggregateAsync(pdfInputStream, linearizationDictionary);

        // 3. Get trailer, if it exists
        var trailer = await GetTrailerAsync(pdfInputStream);

        // 4. Get xref stream dictionary, if it exists
        var xrefStream = await GetXrefStreamAsync(pdfInputStream);

        // 5. Get the trailer dictionary, either from the trailer, or the xref stream dictionary
        var trailerDictionary = trailer?.Dictionary
            ?? ((IStreamObject<IStreamDictionary>)xrefStream.Object).Dictionary as ITrailerDictionary
            ?? throw new ParserException("Unable to find trailer dictionary");

        var documentCatalog = await indirectObjectDictionary.GetAsync<DocumentCatalogDictionary>(trailerDictionary.Root)
            ?? throw new ParserException("Unable to find document catalog dictionary");

        return new ReadOnlyPdf(pdfInputStream, documentCatalog!, trailer, xrefStream, indirectObjectDictionary, linearizationDictionary);
    }

    private static async Task<Trailer?> GetTrailerAsync(Stream pdfStream)
    {
        Logger.Log(LogLevel.Trace, $"Searching for root trailer");

        var xrefObject = await GetXrefObjectAsync(pdfStream);

        if (xrefObject is not Keyword k || k != Constants.Xref)
        {
            return null;
        }

        var objectFinder = new ObjectFinder();

        var trailerOffset = await objectFinder.FindAsync(pdfStream, Constants.Trailer, forwards: false);

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
            && io.Object is IStreamObject<IStreamDictionary> so
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
