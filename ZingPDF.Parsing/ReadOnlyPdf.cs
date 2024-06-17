using ZingPDF.Linearization;
using ZingPDF.Logging;
using ZingPDF.ObjectModel;
using ZingPDF.ObjectModel.DocumentStructure;
using ZingPDF.ObjectModel.DocumentStructure.PageTree;
using ZingPDF.ObjectModel.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.ObjectModel.FileStructure.Trailer;
using ZingPDF.ObjectModel.Objects;
using ZingPDF.ObjectModel.Objects.IndirectObjects;
using ZingPDF.ObjectModel.Objects.Streams;
using ZingPDF.Parsing.Parsers;

namespace ZingPDF.Parsing;

internal class ReadOnlyPdf : IPdf, IDisposable
{
    private readonly Stream _pdfInputStream;
    private readonly LinearizationParameterDictionary? _linearizationDictionary;

    private ReadOnlyPdf(
        Stream pdfInputStream,
        DocumentCatalogDictionary documentCatalog,
        ReadOnlyIndirectObjectDictionary indirectObjectDictionary,
        LinearizationParameterDictionary? linearizationDictionary,
        Trailer? trailer,
        ITrailerDictionary trailerDictionary
        )
    {
        _pdfInputStream = pdfInputStream ?? throw new ArgumentNullException(nameof(pdfInputStream));

        DocumentCatalog = documentCatalog ?? throw new ArgumentNullException(nameof(documentCatalog));
        IndirectObjects = indirectObjectDictionary ?? throw new ArgumentNullException(nameof(indirectObjectDictionary));

        _linearizationDictionary = linearizationDictionary;
        Trailer = trailer;
        TrailerDictionary = trailerDictionary ?? throw new ArgumentNullException(nameof(trailerDictionary));

        PageTree = new PageTree(documentCatalog.Pages, IndirectObjects);
    }

    internal ReadOnlyIndirectObjectDictionary IndirectObjects { get; }

    internal DocumentCatalogDictionary DocumentCatalog { get; }

    internal Trailer? Trailer { get; }

    internal ITrailerDictionary TrailerDictionary { get; }

    internal PageTree PageTree { get; }

    /// <summary>
    /// PDF is linearized if there is a linearization dictionary, AND
    /// the length value (L) is identical to the length of the stream.
    /// A mismatch indicates the file has had at least one incremental update applied,
    /// and should be considered to not be linearized.
    /// </summary>
    internal bool Linearized => _linearizationDictionary != null && _linearizationDictionary.L == _pdfInputStream.Length;

    public async Task CopyToAsync(Stream outputStream)
    {
        _pdfInputStream.Position = 0;
        await _pdfInputStream.CopyToAsync(outputStream);
    }

    #region IPdf

    public Task<int> GetPageCountAsync() => PageTree.GetPageCountAsync();

    public async Task<Page> GetPageAsync(int pageNumber) => (await PageTree.GetAsync(pageNumber)).Get<Page>();

    #endregion

    public static async Task<ReadOnlyPdf> OpenAsync(Stream pdfInputStream)
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
            ?? xrefStream?.Dictionary as ITrailerDictionary
            ?? throw new ParserException("Unable to find trailer dictionary");

        var documentCatalog = await indirectObjectDictionary.GetAsync<DocumentCatalogDictionary>(trailerDictionary.Root);

        return new ReadOnlyPdf(pdfInputStream, documentCatalog!, indirectObjectDictionary, linearizationDictionary, trailer, trailerDictionary);
    }

    #region Private

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

    private static async Task<IStreamObject<IStreamDictionary>?> GetXrefStreamAsync(Stream pdfStream)
    {
        Logger.Log(LogLevel.Trace, $"Searching for root trailer dictionary");

        var xrefObject = await GetXrefObjectAsync(pdfStream);

        if (xrefObject is IndirectObject io
            && io.Children.First() is IStreamObject<IStreamDictionary> so
            && so.Dictionary is CrossReferenceStreamDictionary)
        {
            Logger.Log(LogLevel.Trace, $"Found cross reference stream dictionary");

            return so;
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

        List<PdfObject> items = [];

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

            if (item is IndirectObject o && o.Children.FirstOrDefault() is LinearizationParameterDictionary dict)
            {
                Logger.Log(LogLevel.Trace, $"Found linearisation dictionary");

                return dict;
            }
        }

        Logger.Log(LogLevel.Trace, $"No linearisation dictionary found");

        return null;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ((IDisposable)_pdfInputStream).Dispose();
        }
    }

    #endregion
}

///// <summary>
///// 
///// </summary>
///// <remarks>
///// This class is disposable. Disposing will dispose the underlying <see cref="Stream"/>.
///// </remarks>
//public class Pdf : IDisposable
//{
//    public async Task DeletePageAsync(int pageNumber)
//    {
//        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

//        // TODO: check if there's a more efficient way to do this.
//        var pages = await _pdfNavigator.GetPagesAsync();

//        var pageIndirectObject = pages.ElementAt(pageNumber - 1);
//        var page = (pageIndirectObject.Children.First() as Page)!;
//        var parentIndirectObject = await _pdfNavigator.DereferenceIndirectObjectAsync(page.Parent);
//        var parent = (parentIndirectObject.Children.First() as PageTreeNode)!;

//        parent.Kids = parent.Kids.Cast<IndirectObjectReference>().Where(x => x.Id != pageIndirectObject.Id).ToArray();
//        parent.PageCount--;

//        _pdfNavigator.DeleteObject(pageIndirectObject.Id);
//        _pdfNavigator.UpdateObject(new IndirectObject(parentIndirectObject.Id, parent));
//    }

//    public async Task SetPageRotationAsync(int pageNumber, Rotation rotation)
//    {
//        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
//        ArgumentNullException.ThrowIfNull(rotation);

//        // TODO: check if there's a more efficient way to do this.
//        var pages = await _pdfNavigator.GetPagesAsync();

//        var page = pages.ElementAt(pageNumber - 1);

//        (page.Children.First() as Page)!.Rotate = rotation;

//        _pdfNavigator.UpdateObject(page);
//    }
//}
