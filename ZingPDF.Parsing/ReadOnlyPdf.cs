using ZingPDF.Extensions;
using ZingPDF.Linearization;
using ZingPDF.Logging;
using ZingPDF.ObjectModel;
using ZingPDF.ObjectModel.DocumentStructure;
using ZingPDF.ObjectModel.DocumentStructure.PageTree;
using ZingPDF.ObjectModel.FileStructure.CrossReferences;
using ZingPDF.ObjectModel.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.ObjectModel.FileStructure.Trailer;
using ZingPDF.ObjectModel.Objects;
using ZingPDF.ObjectModel.Objects.IndirectObjects;
using ZingPDF.ObjectModel.Objects.Streams;
using ZingPDF.Parsing.Parsers;

namespace ZingPDF.Parsing;

public class ReadOnlyPdf : IPdf, IDisposable
{
    private readonly Stream _pdfInputStream;
    private readonly LinearizationParameterDictionary? _linearizationDictionary;
    private readonly Trailer? _trailer;

    private readonly PageTree _pageTree;

    private ReadOnlyPdf(
        Stream pdfInputStream,
        DocumentCatalogDictionary documentCatalog,
        LinearizationParameterDictionary? linearizationDictionary,
        Trailer? trailer,
        ITrailerDictionary trailerDictionary,
        Dictionary<int, CrossReferenceEntry> xrefs
        )
    {
        _pdfInputStream = pdfInputStream ?? throw new ArgumentNullException(nameof(pdfInputStream));

        IndirectObjects = new ReadOnlyIndirectObjectDictionary(_pdfInputStream, xrefs);

        DocumentCatalog = documentCatalog ?? throw new ArgumentNullException(nameof(documentCatalog));
        _linearizationDictionary = linearizationDictionary;
        _trailer = trailer;
        TrailerDictionary = trailerDictionary ?? throw new ArgumentNullException(nameof(trailerDictionary));

        _pageTree = new PageTree(documentCatalog.Pages, IndirectObjects);
    }

    internal ReadOnlyIndirectObjectDictionary IndirectObjects { get; }

    internal DocumentCatalogDictionary DocumentCatalog { get; }

    internal ITrailerDictionary TrailerDictionary { get; }

    #region IPdf

    public Task<int> GetPageCountAsync() => _pageTree.GetPageCountAsync();

    public Task<Page> GetPageAsync(int pageNumber) => _pageTree.GetAsync(pageNumber);

    #endregion

    public static async Task<ReadOnlyPdf> LoadAsync(Stream pdfInputStream)
    {
        if (!pdfInputStream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable", nameof(pdfInputStream));
        }

        // Parse PDF for various core elements

        // 1. Is there a linearization parameter dictionary
        var linearizationDictionary = await GetLinearizationDictionaryAsync(pdfInputStream);

        // 2. Aggregate cross references to get the latest versions of all indirect objects
        var xrefs = await AggregateCrossReferencesAsync(pdfInputStream, linearizationDictionary);

        // 3. Get trailer, if it exists
        var trailer = await GetTrailerAsync(pdfInputStream);

        // 4. Get xref stream dictionary, if it exists
        var xrefStream = await GetXrefStreamAsync(pdfInputStream);

        // 5. Get the trailer dictionary, either from the trailer, or the xref stream dictionary
        var trailerDictionary = trailer?.Dictionary
            ?? xrefStream?.Dictionary as ITrailerDictionary
            ?? throw new ParserException("Unable to find trailer dictionary");

        var indirectObjectDictionary = new ReadOnlyIndirectObjectDictionary(pdfInputStream, xrefs);
        var documentCatalog = await indirectObjectDictionary.GetAsync<DocumentCatalogDictionary>(trailerDictionary.Root);

        return new ReadOnlyPdf(pdfInputStream, documentCatalog!, linearizationDictionary, trailer, trailerDictionary, xrefs);
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

    private static async Task<Dictionary<int, CrossReferenceEntry>> AggregateCrossReferencesAsync(
        Stream pdfStream,
        LinearizationParameterDictionary? linearizationDictionary
        )
    {
        Logger.Log(LogLevel.Trace, $"Aggregating cross references");

        Dictionary<int, CrossReferenceEntry> xrefs = [];

        // To aggregate all cross references
        // - check if PDF is linearized
        // - - presence of linearization dictionary in first 1024 bytes
        // - - dictionary L value is identical to stream length
        // - search for startxref keyword
        // - - for linearized files, search from top
        // - - for non-linearized files, search from bottom
        // - following the startxref keyword is a byte offset
        // - go to offset and identify
        // - - if we find the keyword xref, it's a table
        // - - if we find an indirect object containing a stream, it's an xref stream

        // PDF is linearized if there is a linearization dictionary, AND
        // the length value (L) is identical to the length of the stream.
        // A mismatch indicates the file has had at least one incremental update applied,
        // and should be considered to not be linearized.
        var isLinearized = linearizationDictionary != null && linearizationDictionary.L == pdfStream.Length;

        if (linearizationDictionary != null && !isLinearized)
        {
            Logger.Log(LogLevel.Trace, "Treating file as non-linearised, as it has been updated since linearisation.");
        }

        // First, find the startxref keyword
        var offset = await new ObjectFinder().FindAsync(pdfStream, Constants.StartXref, forwards: isLinearized)
            ?? throw new InvalidOperationException($"{Constants.StartXref} not found.");

        pdfStream.Position = offset;
        await pdfStream.AdvanceBeyondNextAsync(Constants.StartXref);

        var xrefOffset = await Parser.For<Integer>().ParseAsync(pdfStream);

        pdfStream.Position = xrefOffset;

        await ParseCrossReferencesAsync(pdfStream, xrefs);

        return xrefs;
    }

    private static async Task ParseCrossReferencesAsync(Stream pdfStream, Dictionary<int, CrossReferenceEntry> xrefs)
    {
        // The offset specified after the startxref keyword will either be an xref table, or stream.
        var type = await TokenTypeIdentifier.TryIdentifyAsync(pdfStream)
            ?? throw new InvalidOperationException("Unable to find cross reference table or stream. PDF may be corrupt.");

        var item = await Parser.For(type).ParseAsync(pdfStream);

        if (item is IndirectObject io
            && io.Children.First() is IStreamObject<IStreamDictionary> streamObject
            && streamObject.Dictionary is CrossReferenceStreamDictionary)
        {
            //UsingXrefStreams = true;
            await ParseCrossReferenceStreamAsync(pdfStream, streamObject, xrefs);
        }
        else if (item is Keyword k && k == Constants.Xref)
        {
            //UsingXrefTables = true;
            await ParseCrossReferenceTableAsync(pdfStream, xrefs);
        }
        else
        {
            throw new InvalidOperationException("Unable to find PDF cross references.");
        }
    }

    private static async Task ParseCrossReferenceStreamAsync(Stream pdfStream, IStreamObject<IStreamDictionary> crossReferenceStream, Dictionary<int, CrossReferenceEntry> xrefs)
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

        var xrefData = await (await crossReferenceStream.GetDecompressedDataAsync()).ReadToEndAsync();
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
        var xrefTable = await Parser.For<CrossReferenceTable>().ParseAsync(pdfStream);

        foreach (var section in xrefTable.Sections)
        {
            var maxIndex = section.Index.StartIndex + section.Entries.Count;

            for (var i = section.Index.StartIndex; i < maxIndex; i++)
            {
                var entry = section.Entries[i];

                xrefs.TryAdd(i, entry);
            }
        }

        var trailer = await Parser.For<Trailer>().ParseAsync(pdfStream);

        if (trailer.Dictionary.Prev is not null)
        {
            pdfStream.Position = trailer.Dictionary.Prev;

            await ParseCrossReferencesAsync(pdfStream, xrefs);
        }
    }

    private static async Task<LinearizationParameterDictionary?> GetLinearizationDictionaryAsync(Stream pdfStream)
    {
        Logger.Log(LogLevel.Trace, $"Searching for linearisation dictionary");

        pdfStream.Position = 0;

        static bool isLinearizationDictionary(IndirectObject o) =>
            o.Children.FirstOrDefault() is LinearizationParameterDictionary;

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

            if (item is IndirectObject o && isLinearizationDictionary(o))
            {
                Logger.Log(LogLevel.Trace, $"Found linearisation dictionary");

                return o.Children.First()! as LinearizationParameterDictionary;
            }
        }

        Logger.Log(LogLevel.Trace, $"No linearisation dictionary found");

        return null;
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
//    /// <summary>
//    /// Save the PDF to the provided output stream.
//    /// </summary>
//    /// <remarks>
//    /// If the PDF has been modified, this method will apply all updates as an incremental update to the PDF, thereby preserving file history. <para></para>
//    /// </remarks>
//    /// <param name="outputStream">The <see cref="Stream"/> to which to write the PDF.</param>
//    /// <param name="saveOptions">PDF save options.</param>
//    /// <exception cref="ArgumentNullException"></exception>
//    /// <exception cref="ArgumentException"></exception>
//    /// <exception cref="InvalidOperationException"></exception>
//    public async Task SaveAsync(Stream outputStream, PdfSaveOptions? saveOptions = null)
//    {
//        ArgumentNullException.ThrowIfNull(outputStream);
//        if (!outputStream.CanWrite) throw new ArgumentException("Provided output stream must be writable", nameof(outputStream));

//        saveOptions ??= PdfSaveOptions.Default;

//        // Copy original PDf to output if required.
//        if (outputStream.Length == 0)
//        {
//            _pdfInputStream.Position = 0;
//            await _pdfInputStream.CopyToAsync(outputStream);
//        }

//        var latestUpdate = _pdfNavigator.GetWorkingIncrementalUpdate();

//        if (latestUpdate.NewOrUpdatedObjects.Count != 0 || latestUpdate.DeletedObjects.Count != 0)
//        {
//            await latestUpdate.WriteAsync(outputStream);
//        }

//        await outputStream.FlushAsync();
//    }

//    /// <summary>
//    /// Get the total number of pages in the PDF.
//    /// </summary>
//    /// <returns>An integer value equal to the total number of pages in the PDF.</returns>
//    public async Task<int> GetPageCountAsync()
//    {
//        var rootPageTreeNodeIndirectObject = await _pdfNavigator.GetRootPageTreeNodeAsync();

//        var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

//        return rootPageTreeNode.PageCount;
//    }

//    /// <summary>
//    /// Get the <see cref="Page"/> at the specified number.<para></para>
//    /// </summary>
//    /// <param name="pageNumber">The page number to return. Pages start at number 1 for the first page.</param>
//    /// <returns>a <see cref="Page"/> instance representing the page at the specified number.</returns>
//    /// <exception cref="ArgumentOutOfRangeException"></exception>
//    public async Task<Page> GetPageAsync(int pageNumber)
//    {
//        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

//        // TODO: check if there's a more efficient way to do this.
//        var pages = await _pdfNavigator.GetPagesAsync();

//        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageNumber, pages.Count());

//        return (pages.ElementAt(pageNumber - 1).Children.First() as Page)!;
//    }

//    /// <summary>
//    /// Append a blank page to the end of the document.
//    /// </summary>
//    public async Task AppendPageAsync(Page.PageCreationOptions? pageCreationOptions = null)
//    {
//        pageCreationOptions ??= Page.PageCreationOptions.Default;

//        var rootPageTreeNodeIndirectObject = await _pdfNavigator.GetRootPageTreeNodeAsync();

//        var page = Page.CreateNew(rootPageTreeNodeIndirectObject.Id.Reference, pageCreationOptions);

//        var pageIndirectObject = await _pdfNavigator.AddNewObjectAsync(page);

//        var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

//        // TODO: For now, to simplify adding pages,
//        // new pages are appended to the root page tree node.
//        // Determine if there's a better way, like ensuring a balanced tree.
//        rootPageTreeNode.Kids.Add(pageIndirectObject.Id.Reference);

//        rootPageTreeNode.PageCount++;

//        _pdfNavigator.UpdateObject(rootPageTreeNodeIndirectObject);
//    }

//    /// <summary>
//    /// Insert a blank page at the specified page number.
//    /// </summary>
//    /// <param name="pageNumber">The page number at which to insert the page.<para></para>
//    /// Pages start at number 1 for the first page.</param>
//    /// <exception cref="ArgumentOutOfRangeException"></exception>
//    public async Task InsertPageAsync(int pageNumber, Page.PageCreationOptions? pageCreationOptions = null)
//    {
//        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

//        pageCreationOptions ??= Page.PageCreationOptions.Default;

//        var count = await GetPageCountAsync();

//        if (pageNumber > count)
//        {
//            throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} must be less than or equal to the total number of pages. To add a page to the end of the PDF, use {nameof(AppendPageAsync)}");
//        }

//        // TODO: check if there's a more efficient way to do this.
//        var pages = await _pdfNavigator.GetPagesAsync();

//        // Find the page, find its parent, insert new page into kids property
//        var pageAtNumberIndirectObject = pages.ElementAt(pageNumber - 1);
//        var pageAtNumber = (pageAtNumberIndirectObject.Children.First() as Page)!;
//        var parentPageTreeNodeIndirectObject = await _pdfNavigator.DereferenceIndirectObjectAsync(pageAtNumber.Parent);
//        var parentPageTreeNode = (parentPageTreeNodeIndirectObject.Children.First() as PageTreeNode)!;

//        var kidsIndex = parentPageTreeNode.Kids.ToList().IndexOf(pageAtNumberIndirectObject.Id.Reference);

//        // Ensure page has all required properties.
//        // required, inheritable properties (Resources, MediaBox) must be set on this or any ancestor
//        // TODO: if linearized, required properties may need to be set on all pages. (7.7.3.4 Inheritance of page attributes)
//        if (pageCreationOptions.MediaBox is null && !await AncestorHasMediaBox(parentPageTreeNode))
//        {
//            throw new Exception("This PDF does not have a default page size, you must therefore provide a PageCreationOptions.MediaBox property or ensure an ancestor has a value for this property."); // TODO: proper exception
//        }

//        var page = Page.CreateNew(
//            parentPageTreeNodeIndirectObject.Id.Reference,
//            pageCreationOptions
//            );

//        var newPageIndirectObject = await _pdfNavigator.AddNewObjectAsync(page);

//        var newKids = parentPageTreeNode.Kids.ToList();
//        newKids.Insert(kidsIndex, newPageIndirectObject.Id.Reference);

//        parentPageTreeNode.Kids = newKids.ToArray();
//        parentPageTreeNode.PageCount++;

//        await IncrementPageCountAsync(parentPageTreeNode);

//        _pdfNavigator.UpdateObject(parentPageTreeNodeIndirectObject);
//    }

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

//    public void Draw(
//        int pageNumber,
//        IEnumerable<Drawing.Path> paths,
//        IEnumerable<Text> text,
//        IEnumerable<Image> imageOperations,
//        CoordinateSystem coordinateSystem = CoordinateSystem.BottomUp
//        )
//    {
//        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

//        throw new NotImplementedException();
//    }

//    public void CompleteForm(IDictionary<string, string> values)
//    {
//        throw new NotImplementedException();
//    }

//    public IDictionary<string, string?> GetFields()
//    {
        

//        throw new NotImplementedException();
//    }

//    public void AddWatermark()
//    {
//        throw new NotImplementedException();
//    }

//    public void Compress(int dpi = 144, int quality = 75)
//    {
//        throw new NotImplementedException();
//    }

//    public void Encrypt()
//    {
//        throw new NotImplementedException();
//    }

//    public void Decrypt()
//    {
//        throw new NotImplementedException();
//    }

//    public void Sign()
//    {
//        throw new NotImplementedException();
//    }

//    public void AppendPdf(Stream stream)
//    {
//        throw new NotImplementedException();
//    }

//    public void Dispose()
//    {
//        Dispose(true);
//        GC.SuppressFinalize(this);
//    }

//    protected virtual void Dispose(bool disposing)
//    {
//        if (disposing)
//        {
//            ((IDisposable)_pdfInputStream).Dispose();
//        }
//    }
    
//    // TODO: move to testable class?
//    /// <summary>
//    /// Recursively walk up the page tree to check for the presence of a MediaBox property.
//    /// </summary>
//    private async Task<bool> AncestorHasMediaBox(PageTreeNode parentPageTreeNode)
//    {
//        if (parentPageTreeNode.MediaBox is not null)
//        {
//            return true;
//        }

//        if (parentPageTreeNode.Parent is null)
//        {
//            return false;
//        }

//        var parent = await _pdfNavigator.DereferenceIndirectObjectAsync<PageTreeNode>(parentPageTreeNode.Parent);
//        if (parent == null)
//        {
//            return false;
//        }

//        if (await AncestorHasMediaBox(parent))
//        {
//            return true;
//        }

//        return false;
//    }

//    // TODO: move to testable class?
//    /// <summary>
//    /// Recursively increment the page count of this page tree node and all its ancestors
//    /// </summary>
//    private async Task IncrementPageCountAsync(PageTreeNode pageTreeNode)
//    {
//        if (pageTreeNode.Parent is null)
//        {
//            return;
//        }

//        var parentPageTreeNodeIndirectObject = await _pdfNavigator.DereferenceIndirectObjectAsync(pageTreeNode.Parent);
//        var parentPageTreeNode = (parentPageTreeNodeIndirectObject.Children.First() as PageTreeNode)!;

//        parentPageTreeNode.PageCount++;

//        _pdfNavigator.UpdateObject(parentPageTreeNodeIndirectObject);

//        await IncrementPageCountAsync(parentPageTreeNode);
//    }
//}
