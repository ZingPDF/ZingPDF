using System.IO;
using System.Xml.Linq;
using ZingPdf.Core;
using ZingPdf.Core.Drawing;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.DataStructures;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Pages;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;
using ZingPdf.Core.Parsing;

namespace ZingPdf
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
        private readonly Stream _stream;
        private List<IndirectObject> _newAndUpdatedObjects = new();

        //private readonly PdfTraversal _pdfTraversal = new();
        //private readonly IndirectObjectManager _indirectObjectManager = new();

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
            IndirectObjectManager indirectObjectManager = new();

            var documentCatalogId = indirectObjectManager.ReserveId();
            var pageTreeNodeIndex = indirectObjectManager.ReserveId();

            var pages = new[] { indirectObjectManager.Create(Page.CreateNew(pageTreeNodeIndex.Reference)) };

            var rootPageTreeNode = indirectObjectManager.Create(pageTreeNodeIndex, PageTreeNode.CreateNew(pages.Select(p => p.Id.Reference).ToArray()));
            var documentCatalog = indirectObjectManager.Create(documentCatalogId, DocumentCatalog.CreateNew(pageTreeNodeIndex.Reference));

            var ms = new MemoryStream();

            new Header().WriteAsync(ms).Wait();

            foreach (var item in indirectObjectManager.Values)
            {
                item.WriteAsync(ms).Wait();
            }

            var xrefEntries = new List<CrossReferenceEntry>
            {
                new CrossReferenceEntry(0, 65535, false)
            };

            xrefEntries.AddRange(indirectObjectManager.Select(i => new CrossReferenceEntry(i.Value.ByteOffset!.Value, i.Value.Id.GenerationNumber, inUse: true)));

            var xrefTable = new CrossReferenceTable(new[]
            {
                // An unmodified PDF has only one cross reference section
                new CrossReferenceSection(0, xrefEntries)
            });

            xrefTable.WriteAsync(ms).Wait();

            var id = HexadecimalString.FromHexStringValue(Guid.NewGuid().ToString("N"));

            new Trailer(
                TrailerDictionary.CreateNew(
                    size: indirectObjectManager.Count,
                    prev: null,
                    root: documentCatalogId.Reference,
                    encrypt: null,
                    info: null,
                    id: new[] { id, id }
                    ),
                    xrefTable.ByteOffset
                )
                .WriteAsync(ms).Wait();

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

            var pdfTraversal = new StreamPdfTraversal(_stream);

            var trailer = await pdfTraversal.GetLatestTrailerAsync();

            _stream.Position = 0;

            await _stream.CopyToAsync(outputStream);
            await _stream.WriteNewLineAsync();

            foreach(var indirectObject in _newAndUpdatedObjects)
            {
                await indirectObject.WriteAsync(outputStream);
            }

            // TODO: account for the use of features which should increase the pdf version

            var xrefEntries = _newAndUpdatedObjects.Select(i =>
                new CrossReferenceEntry(i.ByteOffset!.Value, i.Id.GenerationNumber, inUse: true)
                );

            var xrefTable = new CrossReferenceTable(new[]
            {
                new CrossReferenceSection(0, xrefEntries)
            });

            await outputStream.FlushAsync();
        }

        public int GetPageCount()
        {
            throw new NotImplementedException();
        }

        public Page GetPage(int pageNumber)
        {
            throw new NotImplementedException();
        }

        public void AppendPage()
        {
            throw new NotImplementedException();
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
            IEnumerable<Core.Drawing.Path> paths,
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
