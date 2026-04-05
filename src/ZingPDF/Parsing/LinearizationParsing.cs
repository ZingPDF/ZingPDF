

//// Commenting this while I test if it works with the original parsers
////pdfInputStream.Position = 0;
////using var reader = new StreamReader(pdfInputStream, Encoding.ASCII, leaveOpen: true);

////var content = await GetFirstKBOfFileAsync(reader);

////var linearizationKeyPosition = FindLinearizationKey(content);

////var fileHasLinearizationDictionary = linearizationKeyPosition != null;
////if (fileHasLinearizationDictionary)
////{
////    var objStartIndex = FindStartOfCurrentObject(content, linearizationKeyPosition!.Value);

////    var dictionaryContent = GetDictionaryAsString(content, objStartIndex);

////    var stringDictionary = ParseToDictionaryOfStrings(dictionaryContent);

////    linearizationDictionary = ConvertToTypedLinearizationDictionary(stringDictionary);

////    var dictEnd = objStartIndex + dictionaryContent.Length;
////    var objEndIndex = FindEndOfLinearizationDictionaryObject(content, dictEnd);

////    pdfInputStream.Position = objEndIndex;
////}

////var items = await Parser.PdfObjectGroups.ParseAsync(new SubStream(pdfInputStream, 0, 1024), HoneyTrapIndirectObjectDictionary.Instance);

////var linearizationDictionaryObject = (IndirectObject?)items.Objects
////    .FirstOrDefault(x => x is IndirectObject o && o.Object is LinearizationParameterDictionary);

////linearizationDictionary = linearizationDictionaryObject?.Object as LinearizationParameterDictionary;
/////


//using ZingPDF.IncrementalUpdates;
//using ZingPDF.Linearization;
//using ZingPDF.Logging;
//using ZingPDF.Parsing.Parsers;
//using ZingPDF.Syntax;
//using ZingPDF.Syntax.DocumentStructure;
//using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
//using ZingPDF.Syntax.FileStructure.Trailer;
//using ZingPDF.Syntax.Objects;
//using ZingPDF.Syntax.Objects.IndirectObjects;
//using ZingPDF.Syntax.Objects.Streams;

//namespace ZingPDF.Parsing;

//public class PdfParser
//{
//    public static Task<Pdf> OpenAsync(string filePath)
//    {
//        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

//        return OpenAsync(File.Open(filePath, FileMode.Open));
//    }

//    public static async Task<Pdf> OpenAsync(Stream pdfInputStream)
//    {
//        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));

//        if (!pdfInputStream.CanSeek)
//        {
//            throw new ArgumentException("Stream must be seekable", nameof(pdfInputStream));
//        }

//        var source = await OpenReadOnlyAsync(pdfInputStream);

//        return new Pdf(
//            pdfInputStream,
//            source.DocumentCatalog,
//            source.Trailer,
//            source.CrossReferenceStream,
//            new IndirectObjectManager(source.IndirectObjects),
//            source.LinearizationDictionary
//            );
//    }

//    public static async Task<ReadOnlyPdf> OpenReadOnlyAsync(Stream pdfInputStream)
//    {
//        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));

//        if (!pdfInputStream.CanSeek)
//        {
//            throw new ArgumentException("Stream must be seekable", nameof(pdfInputStream));
//        }

//        // Parsing process:
//        // The goal of parsing is to index all elements of the PDF which are needed to traverse the file.
//        //
//        // - Root Trailer dictionary
//        //   This is the first key element we need to find. There could be multiple trailers in the file.
//        //   There are a few places it could be.
//        //     1: At the tail end of the file as part of a standard trailer in the form of a table
//        //     2: At the tail end of the file as part of a cross reference stream
//        //     3: If the file is linearized, it will be near the top of the file, and it could be a table or stream
//        //       a: In this case there will be a 2nd trailer near the end of the file. In practice, the top one is the root
//        //          and will contain a `Prev` property pointing to this one.
//        //   The process for finding the root trailer dictionary is to parse the file from the top to find and process all
//        //   linearized elements. This is handled by the LinearizationParser. If not linearized, we get the trailer dictionary
//        //   from the bottom of the file.
//        //
//        // - Cross references
//        //   Each trailer or cross reference stream can be considered an update to the file. It contains
//        //   references to new, updated, and deleted objects. And each trailer forms part of a chain which goes all the way 
//        //   back to the first version of the file. The `Prev` property of the trailer dictionary points to the previous
//        //   trailer. During parsing, we take each set of cross references and store it as a distinct version. This allows us
//        //   to restore previous versions.
//        //
//        // - Aggregated cross references
//        //   Since each version only contains the delta of an update, all PDF operations must happen using an aggregate of all
//        //   previous versions. Cross reference aggregation will happen in the DocumentVersionAggregator when the first object
//        //   is requested.
//        //
//        // - Document catalog
//        //   This is referenced by a property of the root trailer dictionary called `Root`. We can only dereference this once 
//        //   the cross references have been aggregated.

//        // Parse linearization elements
//        //var linearizationParser = new LinearizationParser(pdfInputStream);
//        //await linearizationParser.ProcessAsync();

//        //Trailer? rootTrailer;
//        //ITrailerDictionary rootTrailerDictionary;

//        //if (linearizationParser.IsLinearized)
//        //{
//        //    rootTrailer = linearizationParser.Trailer;
//        //    rootTrailerDictionary = linearizationParser.TrailerDictionary;
//        //}
//        //else
//        //{
//        //    rootTrailer = await GetFooterTrailerAsync(pdfInputStream, HoneyTrapIndirectObjectDictionary.Instance);

//        //    if (rootTrailer == null)
//        //    {
//        //        // In a non-linearized file, if there is not trailer, there must be a cross reference stream.
//        //        var xrefStream = await GetXrefStreamAsync(pdfInputStream, HoneyTrapIndirectObjectDictionary.Instance);

//        //        rootTrailerDictionary = (ITrailerDictionary)((StreamObject<IStreamDictionary>)xrefStream?.Object!).Dictionary;
//        //    }
//        //    else
//        //    {
//        //        rootTrailerDictionary = rootTrailer.Dictionary;
//        //    }
//        //}

//        //var documentVersions = await GetDocumentVersionsAsync();


//        //return linearizationDictionary != null
//        //    ? await ParseLinearizedAsync(pdfInputStream, linearizationDictionary, pdfInputStream.Position)
//        //    : await ParseNonLinearizedAsync(pdfInputStream);



//        // TODO: if linearised, root properties (Root, Info) are only in the topmost trailer
//        // TODO: AND xrefs are wrong at this point if we aggregated from the one at the bottom as it doesn't have a Prev property

//        //var trailerDictionary = trailer?.Dictionary
//        //    ?? ((StreamObject<IStreamDictionary>)xrefStream?.Object!).Dictionary as ITrailerDictionary
//        //    ?? throw new ParserException("Unable to find trailer dictionary");

//        //var documentCatalog = await indirectObjectDictionary.GetAsync<DocumentCatalogDictionary>(trailerDictionary.Root)
//        //    ?? throw new ParserException("Unable to find document catalog dictionary");

//        //return new ReadOnlyPdf(pdfInputStream, documentCatalog!, trailer, xrefStream, indirectObjectDictionary, null);

//        throw new NotImplementedException();
//    }

//    private static async Task<ReadOnlyPdf> ParseLinearizedAsync(
//        Stream pdfInputStream,
//        LinearizationParameterDictionary? linearizationDictionary,
//        long xrefLocation
//        )
//    {
//        var indirectObjectDictionary = await new CrossReferenceAggregator().AggregateAsync(pdfInputStream, xrefLocation);

//        var xrefStream = await GetXrefStreamAsync(pdfInputStream, indirectObjectDictionary);

//        var trailer = await GetLeadingTrailerAsync(pdfInputStream, indirectObjectDictionary);

//        var trailerDictionary = trailer?.Dictionary
//            ?? ((StreamObject<IStreamDictionary>)xrefStream?.Object!).Dictionary as ITrailerDictionary
//            ?? throw new ParserException("Unable to find trailer dictionary");

//        var documentCatalog = await indirectObjectDictionary.GetAsync<DocumentCatalogDictionary>(trailerDictionary.Root)
//            ?? throw new ParserException("Unable to find document catalog dictionary");

//        return new ReadOnlyPdf(pdfInputStream, documentCatalog!, trailer, xrefStream, indirectObjectDictionary, linearizationDictionary);
//    }

//    private static Task<Trailer?> GetFooterTrailerAsync(Stream pdfStream, IPdfEditor pdfEditor)
//        => GetTrailerAsync(pdfStream, false, indirectObjectDictionary);

//    private static Task<Trailer?> GetLeadingTrailerAsync(Stream pdfStream, IPdfEditor pdfEditor)
//        => GetTrailerAsync(pdfStream, true, indirectObjectDictionary);

//    private static async Task<Trailer?> GetTrailerAsync(Stream pdfStream, bool fromTop, IPdfEditor pdfEditor)
//    {
//        Logger.Log(LogLevel.Trace, $"Searching for root trailer");

//        var xrefObject = await GetXrefObjectAsync(pdfStream, indirectObjectDictionary);

//        if (xrefObject is not Keyword k || k != Constants.Xref)
//        {
//            return null;
//        }

//        var objectFinder = new ObjectFinder();

//        var trailerOffset = await objectFinder.FindAsync(pdfStream, Constants.Trailer, forwards: fromTop);

//        if (trailerOffset is null)
//        {
//            return null;
//        }

//        pdfStream.Position = trailerOffset.Value;

//        var trailer = await Parser.Trailers.ParseAsync(pdfStream, indirectObjectDictionary);

//        return trailer;
//    }

//    /// <summary>
//    /// Searches from the end of the file for the startxref keyword, parses the value and returns it.
//    /// </summary>
//    private static async Task<int> GetXrefOffsetAsync(Stream pdfStream)
//    {
//        // startxref
//        // Byte_offset_of_last_cross-reference_section
//        // %%EOF

//        var objectFinder = new ObjectFinder();

//        // First, find the startxref keyword
//        var offset = await objectFinder.FindAsync(pdfStream, Constants.StartXref, forwards: false)
//            ?? throw new InvalidOperationException($"{Constants.StartXref} not found.");

//        pdfStream.Position = offset;

//        _ = await Parser.Keywords.ParseAsync(pdfStream, HoneyTrapIndirectObjectDictionary.Instance);

//        return await Parser.Integers.ParseAsync(pdfStream, HoneyTrapIndirectObjectDictionary.Instance);
//    }

//    private static async Task<IndirectObject?> GetXrefStreamAsync(Stream pdfStream, IPdfEditor pdfEditor)
//    {
//        Logger.Log(LogLevel.Trace, $"Searching for root trailer dictionary");

//        var xrefObject = await GetXrefObjectAsync(pdfStream, indirectObjectDictionary);

//        if (xrefObject is IndirectObject io
//            && io.Object is StreamObject<IStreamDictionary> so
//            && so.Dictionary is CrossReferenceStreamDictionary)
//        {
//            Logger.Log(LogLevel.Trace, $"Found cross reference stream dictionary");

//            return io;
//        }

//        return null;
//    }

//    /// <summary>
//    /// Searches from the end of the file for the startxref keyword, parses the value,
//    /// moves to the discovered offset, parses whatever is there.
//    /// </summary>
//    private static async Task<IPdfObject> GetXrefObjectAsync(Stream pdfStream, IPdfEditor pdfEditor)
//    {
//        var xrefOffset = await GetXrefOffsetAsync(pdfStream);

//        pdfStream.Position = xrefOffset;

//        var type = await TokenTypeIdentifier.TryIdentifyAsync(pdfStream);

//        var item = await Parser.For(type).ParseAsync(pdfStream, indirectObjectDictionary);

//        return item;
//    }

//    private static async Task<LinearizationParameterDictionary?> GetLinearizationDictionaryAsync(Stream pdfStream, IPdfEditor pdfEditor)
//    {
//        Logger.Log(LogLevel.Trace, $"Searching for linearisation dictionary");

//        pdfStream.Position = 0;

//        var limit = Math.Min(1024, pdfStream.Length);

//        while (pdfStream.Position < limit)
//        {
//            var type = await TokenTypeIdentifier.TryIdentifyAsync(pdfStream);
//            if (type is null)
//            {
//                // TODO: is this a valid scenario?
//                break;
//            }

//            var item = await Parser.For(type).ParseAsync(pdfStream, indirectObjectDictionary);

//            if (item is IndirectObject o && o.Object is LinearizationParameterDictionary dict)
//            {
//                Logger.Log(LogLevel.Trace, $"Found linearisation dictionary");

//                return dict;
//            }
//        }

//        return null;
//    }
//}
