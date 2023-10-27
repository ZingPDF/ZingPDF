using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf
{
    public class Pdf
    {
        private readonly Header _header;
        private readonly IndirectObjectManager _indirectObjects = new();

        private readonly IndirectObjectReference _documentCatalogReference;
        private readonly IndirectObjectReference? _infoReference;

        /// <summary>
        /// Create a new blank PDF document.
        /// </summary>
        /// <remarks>
        /// The new document will contain a single blank page by default.
        /// </remarks>
        public Pdf()
        {
            _header = new Header();

            var documentCatalogId = _indirectObjects.ReserveId();
            _documentCatalogReference = documentCatalogId.Reference;

            var pageTreeNodeIndex = _indirectObjects.ReserveId();

            var pages = new[] { _indirectObjects.Create(new Page(pageTreeNodeIndex.Reference)) };

            var pageTreeNode = _indirectObjects.Create(pageTreeNodeIndex, CreatePageTreeNode(pages.Select(p => p.Id.Reference).ToArray()));
            var documentCatalog = _indirectObjects.Create(documentCatalogId, CreateDocumentCatalog(pageTreeNodeIndex.Reference));
        }

        /// <summary>
        /// Used internally to create a PDF from a parsed document.
        /// </summary>
        internal Pdf(
            Header header,
            IEnumerable<IndirectObject> indirectObjects,
            IndirectObjectReference documentCatalogReference,
            IndirectObjectReference? infoReference
            )
        {
            _header = header ?? throw new ArgumentNullException(nameof(header));
            if (indirectObjects is null) throw new ArgumentNullException(nameof(indirectObjects));

            foreach (var indirectObject in indirectObjects)
            {
                _indirectObjects.Add(indirectObject.Id, indirectObject);
            }

            _documentCatalogReference = documentCatalogReference ?? throw new ArgumentNullException(nameof(documentCatalogReference));
            _infoReference = infoReference;
        }

        public async Task<Stream> ToStreamAsync()
        {
            var ms = new MemoryStream();
            await WriteAsync(ms);

            ms.Position = 0;
            return ms;
        }

        public async Task WriteAsync(Stream stream)
        {
            await _header.WriteAsync(stream);

            foreach(var indirectObject in _indirectObjects.Skip(1))
            {
                await indirectObject.Value.WriteAsync(stream);
            }

            var xrefSections = new[]
            {
                // An unmodified PDF has only one cross reference section
                new CrossReferenceSection(0, _indirectObjects.Select(i => new CrossReferenceEntry(i.Value?.ByteOffset!.Value ?? 0, i.Value?.Id.GenerationNumber ?? 0, i.Value != null)))
            };

            var xrefTable = new CrossReferenceTable(xrefSections);
            await xrefTable.WriteAsync(stream);

            await new Trailer(
                _documentCatalogReference,
                xrefTable.ByteOffset!.Value,
                new Integer(_indirectObjects.Count),
                _infoReference
                )
                .WriteAsync(stream);

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
}
