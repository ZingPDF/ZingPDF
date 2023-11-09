using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.Pages;

namespace ZingPdf
{
    public class Pdf
    {
        private readonly PdfTraversal _pdfTraversal = new();
        private readonly IndirectObjectManager _indirectObjectManager = new();

        private readonly Header _header;
        private readonly IEnumerable<PdfIncrement> _increments;

        /// <summary>
        /// Create a new blank PDF document.
        /// </summary>
        /// <remarks>
        /// The new document will contain a single blank page by default.
        /// </remarks>
        public Pdf()
        {
            _header = new();

            var documentCatalogId = _indirectObjectManager.ReserveId();
            var pageTreeNodeIndex = _indirectObjectManager.ReserveId();

            var pages = new[] { _indirectObjectManager.Create(Page.CreateNew(pageTreeNodeIndex.Reference)) };

            var rootPageTreeNode = _indirectObjectManager.Create(pageTreeNodeIndex, PageTreeNode.CreateNew(pages.Select(p => p.Id.Reference).ToArray()));
            var documentCatalog = _indirectObjectManager.Create(documentCatalogId, DocumentCatalog.CreateNew(pageTreeNodeIndex.Reference));

            var xrefEntries = new List<CrossReferenceEntry>
            {
                new CrossReferenceEntry(0, 65535, false)
            };

            xrefEntries.AddRange(_indirectObjectManager.Select(i => new CrossReferenceEntry(i.Value.ByteOffset ?? 0, i.Value.Id.GenerationNumber, inUse: true)));

            var xrefTable = new CrossReferenceTable(new[]
            {
                // An unmodified PDF has only one cross reference section
                new CrossReferenceSection(0, xrefEntries)
            });

            _increments = new[] { new PdfIncrement(_indirectObjectManager.Select(o => o.Value), xrefTable, documentCatalogId.Reference) };
        }

        /// <summary>
        /// Used internally to create a PDF from a parsed document.
        /// </summary>
        internal Pdf(Header header, IEnumerable<PdfIncrement> increments)
        {
            _header = header ?? throw new ArgumentNullException(nameof(header));
            _increments = increments ?? throw new ArgumentNullException(nameof(increments));

            foreach(var item in increments.SelectMany(i => i.Body))
            {
                _indirectObjectManager.Add(item.Id, item);
            }
        }

        public int GetPageCount()
        {
            var trailerDictionary = _pdfTraversal.GetLatestTrailerDictionary(_increments);

            var rootPageTreeNode = _pdfTraversal
                .GetRootPageTreeNode(trailerDictionary, _indirectObjectManager);

            return rootPageTreeNode.PageCount;
        }

        /// <summary>
        /// Get the page at the specified number.
        /// </summary>
        /// <returns>A <see cref="Page"/> instance.</returns>
        public Page GetPage()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add a blank page to the end of the document.
        /// </summary>
        /// <returns>The page number of the new page.</returns>
        public void AppendPage()
        {
            var trailerDictionary = _pdfTraversal.GetLatestTrailerDictionary(_increments);
            var documentCatalog = _pdfTraversal.GetDocumentCatalog(trailerDictionary, _indirectObjectManager);

            var pagesCatalogIndirectObject = _indirectObjectManager[documentCatalog.Pages.Id];
            var page = _indirectObjectManager.Create(Page.CreateNew(pagesCatalogIndirectObject.Id.Reference));

            var rootPageTreeNode = _pdfTraversal.GetRootPageTreeNode(trailerDictionary, _indirectObjectManager);

            // TODO: For now, to simplify adding pages,
            // new pages are appended to the root page tree node.
            // Determine if there's a better way, like ensuring a balanced tree.
            rootPageTreeNode.Pages.Add(page.Id.Reference);

            rootPageTreeNode.PageCount++;

            _increments.Last().Add(page);
        }

        /// <summary>
        /// Insert a blank page at the specified location.
        /// </summary>
        /// <param name="pageNumber">The location at which to insert the page.</param>
        public void InsertPage(int pageNumber)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete a page.
        /// </summary>
        /// <param name="pageNumber">The location of the page to delete.</param>
        public void DeletePage(int pageNumber)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Output the PDF to a new <see cref="MemoryStream"/>.
        /// </summary>
        /// <returns>A <see cref="MemoryStream"/> instance containing the byte data of the PDF. The stream position will be zero.</returns>
        public async Task<MemoryStream> ToStreamAsync()
        {
            var ms = new MemoryStream();
            await WriteAsync(ms);

            ms.Position = 0;
            return ms;
        }

        /// <summary>
        /// Write the PDF to the supplied <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public async Task WriteAsync(Stream stream)
        {
            if (!stream.CanWrite)
            {
                throw new ArgumentException("Provided stream must be writable", nameof(stream));
            }

            await _header.WriteAsync(stream);

            foreach (var increment in _increments)
            {
                await increment.WriteAsync(stream);
            }

            await stream.FlushAsync();
        }
    }
}
