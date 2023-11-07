using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf
{
    public class Pdf
    {
        private readonly IndirectObjectManager _indirectObjects = new();

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

            var documentCatalogId = _indirectObjects.ReserveId();
            var pageTreeNodeIndex = _indirectObjects.ReserveId();

            var pages = new[] { _indirectObjects.Create(new Page(pageTreeNodeIndex.Reference)) };

            var pageTreeNode = _indirectObjects.Create(pageTreeNodeIndex, CreatePageTreeNode(pages.Select(p => p.Id.Reference).ToArray()));
            var documentCatalog = _indirectObjects.Create(documentCatalogId, CreateDocumentCatalog(pageTreeNodeIndex.Reference));

            var xrefEntries = new List<CrossReferenceEntry>
            {
                new CrossReferenceEntry(0, 65535, false)
            };

            xrefEntries.AddRange(_indirectObjects.Select(i => new CrossReferenceEntry(i.Value.ByteOffset ?? 0, i.Value.Id.GenerationNumber, inUse: true)));

            var xrefTable = new CrossReferenceTable(new[]
            {
                // An unmodified PDF has only one cross reference section
                new CrossReferenceSection(0, xrefEntries)
            });

            _increments = new[] { new PdfIncrement(_indirectObjects.Select(o => o.Value), xrefTable, documentCatalogId.Reference) };
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
        public int AppendPage()
        {
            throw new NotImplementedException();
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

            foreach(var increment in _increments)
            {
                await increment.WriteAsync(stream);
            }

            await stream.FlushAsync();
        }

        private static Dictionary CreateDocumentCatalog(IndirectObjectReference pageTreeNode)
        {
            return new Dictionary<Name, PdfObject>()
            {
                { "Type", new Name("Catalog") },
                { "Pages", pageTreeNode },
            };
        }

        private static Dictionary CreatePageTreeNode(IndirectObjectReference[] pages)
        {
            return new Dictionary(new Dictionary<Name, PdfObject>
            {
                { "Type", new Name("Pages") },
                { "Kids", new ArrayObject(pages) },
                { "Count", new Integer(pages.Length) },
            });
        }
    }

    /// <summary>
    /// The body, xref table and trailer for a document.
    /// Each incremental update adds a new PdfIncrement.
    /// </summary>
    internal class PdfIncrement : PdfObject
    {
        private readonly IEnumerable<IndirectObject> _body;
        private readonly CrossReferenceTable _crossReferenceTable;
        private readonly IndirectObjectReference _documentCatalogReference;
        private readonly IndirectObjectReference? _infoReference;
        private readonly ArrayObject? _id;

        internal PdfIncrement(
            IEnumerable<IndirectObject> body,
            CrossReferenceTable crossReferenceTable,
            IndirectObjectReference documentCatalogReference,
            IndirectObjectReference? infoReference = null,
            ArrayObject? id = null
            )
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _crossReferenceTable = crossReferenceTable ?? throw new ArgumentNullException(nameof(crossReferenceTable));
            _documentCatalogReference = documentCatalogReference ?? throw new ArgumentNullException(nameof(documentCatalogReference));
            _infoReference = infoReference;
            _id = id;
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            foreach(var item in _body)
            {
                await item.WriteAsync(stream);
            }

            _crossReferenceTable.UpdateByteOffsets(_body);

            await _crossReferenceTable.WriteAsync(stream);

            await new Trailer(
                _documentCatalogReference,
                _crossReferenceTable.ByteOffset!.Value,
                _body.Count() + 1,
                _infoReference,
                _id
                )
                .WriteAsync(stream);
        }
    }
}
