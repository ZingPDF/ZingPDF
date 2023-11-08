using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf
{
    public class Pdf
    {
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

            var pageTreeNode = _indirectObjectManager.Create(pageTreeNodeIndex, PagesCatalog.CreateNew(pages.Select(p => p.Id.Reference).ToArray()));
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
        }

        /// <summary>
        /// Get the page at the specified number.
        /// </summary>
        /// <returns>A <see cref="Page"/> instance.</returns>
        /// <exception cref="NotImplementedException"></exception>
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
            var pagesCatalogIndirectObject = GetPagesCatalog();
            var page = _indirectObjectManager.Create(Page.CreateNew(pagesCatalogIndirectObject.Id.Reference));

            var pagesCatalog = (pagesCatalogIndirectObject.Children.First() as PagesCatalog)!;

            pagesCatalog.Pages = pagesCatalog.Pages.Append(page.Id.Reference).ToArray();

            _increments.Last().Add(page);
        }

        /// <summary>
        /// Insert a blank page at the specified location.
        /// </summary>
        /// <param name="pageNumber">The location at which to insert the page.</param>
        /// <exception cref="NotImplementedException"></exception>
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

        private IndirectObject GetPagesCatalog()
        {
            var trailerDictionary = GetTrailerDictionary();
            var documentCatalog = GetDocumentCatalog(trailerDictionary);

            var pagesCatalogReference = documentCatalog.Get<IndirectObjectReference>("Pages")!;

            return _indirectObjectManager[pagesCatalogReference.Id];
        }

        private Dictionary GetTrailerDictionary()
        {
            return _increments.Last().Trailer.Dictionary;
        }

        private Dictionary GetDocumentCatalog(Dictionary trailerDictionary)
        {
            var documentCatalogReference = trailerDictionary.Get<IndirectObjectReference>("Root")!;
            
            return _indirectObjectManager.GetSingle<Dictionary>(documentCatalogReference.Id);
        }
    }
}
