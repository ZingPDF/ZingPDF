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

        /// <summary>
        /// Used internally to create a PDF from a parsed document.
        /// </summary>
        internal Pdf(IEnumerable<IndirectObject> indirectObjects, IndirectObjectReference documentCatalogId)
        {
            foreach (var indirectObject in indirectObjects)
            {
                _indirectObjects.Add(indirectObject);
            }
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
            var documentCatalogIndex = _indirectObjects.ReserveId();
            var pageTreeNodeIndex = _indirectObjects.ReserveId();

            var pages = new[] { _indirectObjects.Add(new Page(pageTreeNodeIndex.Reference)) };

            var pageTreeNode = _indirectObjects.SetChild(pageTreeNodeIndex, new PageTreeNode(pages.Select(p => p.Id.Reference).ToArray()));
            var documentCatalog = _indirectObjects.SetChild(documentCatalogIndex, new DocumentCatalog(pageTreeNodeIndex.Reference));

            await new Header().WriteAsync(stream);
            await documentCatalog.WriteAsync(stream);

            var bodyObjects = new List<PdfObject>()
            {
                pageTreeNode,
            };

            bodyObjects.AddRange(pages);

            await new Body(bodyObjects.ToArray()).WriteAsync(stream);

            var xrefSections = new[]
            {
                // An unmodified PDF has only one cross reference section
                new CrossReferenceSection(0, _indirectObjects.Select(i => new CrossReferenceEntry(i.Value?.ByteOffset!.Value ?? 0, i.Value?.Id.GenerationNumber ?? 0, i.Value != null)))
            };

            var xrefTable = new CrossReferenceTable(xrefSections);
            await xrefTable.WriteAsync(stream);
            await new Trailer(documentCatalog.Id.Reference, xrefTable.ByteOffset!.Value, new Integer(_indirectObjects.Count)).WriteAsync(stream);

            await stream.FlushAsync();
        }
    }
}
