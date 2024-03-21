namespace ZingPDF;

internal class Pdf
{
    // TODO: define structure for new PDF, not parsed

    ///// <summary>
    ///// Create a new PDF. The new document will contain a single blank page by default.
    ///// </summary>
    ///// <returns>A <see cref="ParsedPdf"/> instance.</returns>
    //public static Pdf Create()
    //{
    //    var documentCatalogId = new IndirectObjectId(1, 0);
    //    var pageTreeNodeId = new IndirectObjectId(2, 0);
    //    var pageId = new IndirectObjectId(3, 0);

    //    var page = new IndirectObject(pageId, Page.CreateNew(pageTreeNodeId.Reference));
    //    var rootPageTreeNode = new IndirectObject(pageTreeNodeId, PageTreeNode.CreateNew(new[] { page.Id.Reference }));

    //    var documentCatalog = new IndirectObject(documentCatalogId, DocumentCatalog.CreateNew(pageTreeNodeId.Reference));

    //    var ms = new MemoryStream();

    //    new Header().WriteAsync(ms).Wait();

    //    documentCatalog.WriteAsync(ms).Wait();
    //    rootPageTreeNode.WriteAsync(ms).Wait();
    //    page.WriteAsync(ms).Wait();

    //    var xrefTable = new CrossReferenceTable(
    //        new[] { new CrossReferenceSection(0, new[] {
    //        CrossReferenceEntry.RootFreeEntry,
    //        new CrossReferenceEntry(documentCatalog.ByteOffset!.Value, 0, inUse: true, compressed : false),
    //        new CrossReferenceEntry(rootPageTreeNode.ByteOffset!.Value, 0, inUse: true, compressed : false),
    //        new CrossReferenceEntry(page.ByteOffset!.Value, 0, inUse: true, compressed : false),
    //    })
    //    });

    //    xrefTable.WriteAsync(ms).Wait();

    //    HexadecimalString id = Guid.NewGuid().ToString("N");

    //    var trailerDictionary = TrailerDictionary.CreateNew(
    //        size: 4,
    //        prev: null,
    //        root: documentCatalog.Id.Reference,
    //        encrypt: null,
    //        info: null,
    //        id: new ArrayObject(new[] { id, id })
    //        );

    //    var trailer = new Trailer(trailerDictionary, xrefTable.ByteOffset!.Value);
    //    trailer.WriteAsync(ms).Wait();

    //    ms.Position = 0;

    //    return new(ms);
    //}
}
