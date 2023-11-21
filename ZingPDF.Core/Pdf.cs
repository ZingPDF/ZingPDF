using ZingPdf.Core.Drawing;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.DataStructures;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Pages;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core
{
    // When doing incremental update, we need
    //  - last trailer
    //  - last xref table
    //  - access to file stream
    //  - - e.g. to add page
    //  - - seek to root page tree node, parse it
    //  - - add new version of root page tree node to end of file (with extra entry in kids array, incremented generation number)
    //  - - add new page indirect object
    //  - - add new xref section referencing the new objects
    //  - - add new trailer referencing the new xref section and previous

    // When creating new file, we need
    //  - header
    //  - list of indirect objects

    // When compressing, encrypting, decrypting, reading file, we need
    //  - access to all objects

    public class Pdf
    {
        private readonly IncrementalUpdateManager _incrementalUpdateManager = new();
        private readonly Stream _stream;

        private Pdf(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable", nameof(stream));
            }
        }

        /// <summary>
        /// Create a new PDF. The new document will contain a single blank page by default.
        /// </summary>
        /// <returns>A <see cref="Pdf"/> instance.</returns>
        public static Pdf Create()
        {
            var documentCatalogId = new IndirectObjectId(1, 0);
            var pageTreeNodeId = new IndirectObjectId(2, 0);
            var pageId = new IndirectObjectId(3, 0);

            var page = new IndirectObject(pageId, Page.CreateNew(pageTreeNodeId.Reference));
            var rootPageTreeNode = new IndirectObject(pageTreeNodeId, PageTreeNode.CreateNew(new[] { page.Id.Reference }));

            var documentCatalog = new IndirectObject(documentCatalogId, DocumentCatalog.CreateNew(pageTreeNodeId.Reference));

            var ms = new MemoryStream();

            new Header().WriteAsync(ms).Wait();

            documentCatalog.WriteAsync(ms).Wait();
            rootPageTreeNode.WriteAsync(ms).Wait();
            page.WriteAsync(ms).Wait();

            var xrefTable = new CrossReferenceTable(
                new[] { new CrossReferenceSection(0, new[] {
                    new CrossReferenceEntry(0, 65535, inUse: false),
                    new CrossReferenceEntry(documentCatalog.ByteOffset!.Value, 0, inUse: true),
                    new CrossReferenceEntry(rootPageTreeNode.ByteOffset!.Value, 0, inUse: true),
                    new CrossReferenceEntry(page.ByteOffset!.Value, 0, inUse: true),
                })
            });

            xrefTable.WriteAsync(ms).Wait();

            var id = HexadecimalString.FromHexStringValue(Guid.NewGuid().ToString("N"));

            var trailerDictionary = TrailerDictionary.CreateNew(
                size: 4,
                prev: null,
                root: documentCatalog.Id.Reference,
                encrypt: null,
                info: null,
                id: new ArrayObject(new[] { id, id })
                );

            new Trailer(trailerDictionary, xrefTable.ByteOffset!.Value).WriteAsync(ms).Wait();

            ms.Position = 0;

            return new(ms);
        }

        public static Pdf Load(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable", nameof(stream));
            }

            return new(stream);
        }

        public async Task SaveAsync(Stream outputStream, PdfSaveOptions? saveOptions = null)
        {
            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("Provided stream must be writable", nameof(outputStream));
            }

            saveOptions ??= PdfSaveOptions.Default;

            _stream.Position = 0;

            // Copy original PDf to output.
            await _stream.CopyToAsync(outputStream);
            await outputStream.WriteNewLineAsync();

            await _incrementalUpdateManager.SaveAsync(outputStream);
        }

        public int GetPageCount()
        {
            throw new NotImplementedException();
        }

        public Page GetPage(int pageNumber)
        {
            throw new NotImplementedException();
        }

        public async Task AppendPageAsync()
        {
            var pdfTraversal = new StreamPdfTraversal(_stream);

            var trailer = await pdfTraversal.GetLatestTrailerAsync();
            var rootPageTreeNodeIndirectObject = await pdfTraversal.GetRootPageTreeNodeAsync(trailer.Dictionary);

            var page = Page.CreateNew(rootPageTreeNodeIndirectObject.Id.Reference);

            var pageIndirectObject = await _incrementalUpdateManager.AddNewObjectAsync(page, _stream);

            var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

            // TODO: For now, to simplify adding pages,
            // new pages are appended to the root page tree node.
            // Determine if there's a better way, like ensuring a balanced tree.
            rootPageTreeNode.Kids.Add(pageIndirectObject.Id.Reference);

            rootPageTreeNode.PageCount++;

            _incrementalUpdateManager.UpdateObject(rootPageTreeNodeIndirectObject);
        }

        public void InsertPage(int pageNumber)
        {
            throw new NotImplementedException();
        }

        public void ReplacePage(int pageNumber, Page page)
        {
            throw new NotImplementedException();
        }

        public void DeletePage(int pageNumber)
        {
            throw new NotImplementedException();
        }

        public void SetPageRotation(Rotation rotation)
        {
            throw new NotImplementedException();
        }

        public void Draw(
            int pageNumber,
            IEnumerable<Drawing.Path> paths,
            IEnumerable<Text> text,
            IEnumerable<Image> imageOperations,
            CoordinateSystem coordinateSystem = CoordinateSystem.BottomUp
            )
        {
            throw new NotImplementedException();
        }

        public void CompleteForm(IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, string?> GetFields()
        {
            throw new NotImplementedException();
        }

        public void AddWatermark()
        {
            throw new NotImplementedException();
        }

        public void Compress(int dpi = 144, int quality = 75)
        {
            throw new NotImplementedException();
        }

        public void Encrypt()
        {
            throw new NotImplementedException();
        }

        public void Decrypt()
        {
            throw new NotImplementedException();
        }

        public void Sign()
        {
            throw new NotImplementedException();
        }

        public void AppendPdf(Stream stream)
        {
            throw new NotImplementedException();
        }
    }

    //public class Pdfr
    //{
    //    private readonly PdfTraversal _pdfTraversal = new();
    //    private readonly IndirectObjectManager _indirectObjectManager = new();

    //    private readonly Header _header;
    //    private readonly IEnumerable<PdfIncrement> _increments;



    //    /// <summary>
    //    /// Used internally to create a PDF from a parsed document.
    //    /// </summary>
    //    internal Pdfr(Header header, IEnumerable<PdfIncrement> increments)
    //    {
    //        _header = header ?? throw new ArgumentNullException(nameof(header));
    //        _increments = increments ?? throw new ArgumentNullException(nameof(increments));

    //        foreach(var item in increments.SelectMany(i => i.Body))
    //        {
    //            _indirectObjectManager.Add(item.Id, item);
    //        }
    //    }

    //    public int GetPageCount()
    //    {
    //        var trailerDictionary = _pdfTraversal.GetLatestTrailerDictionary(_increments);

    //        var rootPageTreeNode = _pdfTraversal
    //            .GetRootPageTreeNode(trailerDictionary, _indirectObjectManager);

    //        return rootPageTreeNode.PageCount;
    //    }

    //    /// <summary>
    //    /// Get the page at the specified number.
    //    /// </summary>
    //    /// <returns>A <see cref="Page"/> instance.</returns>
    //    public Page GetPage(int pageNumber)
    //    {
    //        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));

    //        var trailerDictionary = _pdfTraversal.GetLatestTrailerDictionary(_increments);

    //        var pages = _pdfTraversal.GetPages(trailerDictionary, _indirectObjectManager);

    //        if (pageNumber > pages.Count()) throw new ArgumentOutOfRangeException(nameof(pageNumber));

    //        return pages.ElementAt(pageNumber - 1);
    //    }

    //    /// <summary>
    //    /// Add a blank page to the end of the document.
    //    /// </summary>
    //    /// <returns>The page number of the new page.</returns>
    //    public void AppendPage()
    //    {
    //        var trailerDictionary = _pdfTraversal.GetLatestTrailerDictionary(_increments);
    //        var documentCatalog = _pdfTraversal.GetDocumentCatalog(trailerDictionary, _indirectObjectManager);

    //        var pagesCatalogIndirectObject = _indirectObjectManager[documentCatalog.Pages.Id];
    //        var page = _indirectObjectManager.Create(Page.CreateNew(pagesCatalogIndirectObject.Id.Reference));

    //        var rootPageTreeNode = _pdfTraversal.GetRootPageTreeNode(trailerDictionary, _indirectObjectManager);

    //        // TODO: For now, to simplify adding pages,
    //        // new pages are appended to the root page tree node.
    //        // Determine if there's a better way, like ensuring a balanced tree.
    //        rootPageTreeNode.Kids.Add(page.Id.Reference);

    //        rootPageTreeNode.PageCount++;

    //        _increments.Last().Add(page);
    //    }

    //    /// <summary>
    //    /// Insert a blank page at the specified location.
    //    /// </summary>
    //    /// <param name="pageNumber">The location at which to insert the page.</param>
    //    public void InsertPage(int pageNumber)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    /// <summary>
    //    /// Delete a page.
    //    /// </summary>
    //    /// <param name="pageNumber">The location of the page to delete.</param>
    //    public void DeletePage(int pageNumber)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    /// <summary>
    //    /// Output the PDF to a new <see cref="MemoryStream"/>.
    //    /// </summary>
    //    /// <returns>A <see cref="MemoryStream"/> instance containing the byte data of the PDF. The stream position will be zero.</returns>
    //    public async Task<MemoryStream> ToStreamAsync()
    //    {
    //        var ms = new MemoryStream();
    //        await WriteAsync(ms);

    //        ms.Position = 0;
    //        return ms;
    //    }

    //    /// <summary>
    //    /// Write the PDF to the supplied <see cref="Stream"/>.
    //    /// </summary>
    //    /// <param name="stream"></param>
    //    /// <returns></returns>
    //    public async Task WriteAsync(Stream stream)
    //    {
    //        if (!stream.CanWrite)
    //        {
    //            throw new ArgumentException("Provided stream must be writable", nameof(stream));
    //        }

    //        await _header.WriteAsync(stream);

    //        foreach (var increment in _increments)
    //        {
    //            await increment.WriteAsync(stream);
    //        }

    //        await stream.FlushAsync();
    //    }
    //}
}
