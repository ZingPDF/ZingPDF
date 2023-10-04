using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf
{
    public class Pdf
    {
        private readonly IndirectObjectCollection _indirectObjects = new();

        //public List<Page> Pages { get; } = new();

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

            //var pages = Pages.Select(p => _indirectObjects.Add(new Page(pageTreeNodeIndex))).ToArray();
            var pages = System.Array.Empty<IndirectObject>();

            var pageTreeNode = _indirectObjects.SetChild(pageTreeNodeIndex, new PageTreeNode(pages.Select(p => p.Id).ToArray()));
            var documentCatalog = _indirectObjects.SetChild(documentCatalogIndex, new DocumentCatalog(pageTreeNodeIndex));

            await new Header().WriteAsync(stream);
            await documentCatalog.WriteAsync(stream);

            var bodyObjects = new List<PdfObject>()
            {
                pageTreeNode,
            };

            bodyObjects.AddRange(pages);

            await new Body(bodyObjects.ToArray()).WriteAsync(stream);

            var xrefTable = new CrossReferenceTable(_indirectObjects);
            await xrefTable.WriteAsync(stream);
            await new Trailer(documentCatalog.Id, xrefTable, _indirectObjects.Count).WriteAsync(stream);

            await stream.FlushAsync();
        }
    }
}
