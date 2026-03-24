using ZingPDF.Elements;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Elements.Forms;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF;

internal static class PdfBootstrapper
{
    public static Pdf Create(Action<PageDictionary.PageCreationOptions>? configureOptions)
    {
        var pageOptions = PageDictionary.PageCreationOptions.Initialize(configureOptions);
        pageOptions.MediaBox ??= Rectangle.FromDimensions(595, 842);

        var stream = CreateDocumentStream(pageOptions);
        return Pdf.Load(stream);
    }

    private static MemoryStream CreateDocumentStream(PageDictionary.PageCreationOptions pageOptions)
    {
        var stream = new MemoryStream();
        var pdfContext = new BootstrapPdfContext();

        var catalogId = new IndirectObjectId(1, 0);
        var pageTreeId = new IndirectObjectId(2, 0);
        var pageId = new IndirectObjectId(3, 0);

        var page = new IndirectObject(
            pageId,
            PageDictionary.CreateNew(new IndirectObjectReference(pageTreeId, ObjectContext.UserCreated), pdfContext, pageOptions));

        var rootPageTreeNode = new IndirectObject(
            pageTreeId,
            PageTreeNodeDictionary.CreateNew(new ArrayObject([page.Reference], ObjectContext.UserCreated), pdfContext));

        var documentCatalog = new IndirectObject(
            catalogId,
            DocumentCatalogDictionary.FromDictionary(
                new Dictionary<string, IPdfObject>
                {
                    [Constants.DictionaryKeys.Type] = (Name)Constants.DictionaryTypes.Catalog,
                    [Constants.DictionaryKeys.DocumentCatalog.Pages] = rootPageTreeNode.Reference,
                },
                pdfContext,
                ObjectContext.UserCreated));

        new Header(2.0, ObjectContext.UserCreated).WriteAsync(stream).GetAwaiter().GetResult();
        documentCatalog.WriteAsync(stream).GetAwaiter().GetResult();
        rootPageTreeNode.WriteAsync(stream).GetAwaiter().GetResult();
        page.WriteAsync(stream).GetAwaiter().GetResult();

        var xref = new CrossReferenceTable(
            [
                new CrossReferenceSection(
                    0,
                    [
                        CrossReferenceEntry.RootFreeEntry,
                        new CrossReferenceEntry(documentCatalog.ByteOffset!.Value, 0, true, false),
                        new CrossReferenceEntry(rootPageTreeNode.ByteOffset!.Value, 0, true, false),
                        new CrossReferenceEntry(page.ByteOffset!.Value, 0, true, false)
                    ],
                    ObjectContext.UserCreated)
            ],
            ObjectContext.UserCreated);

        xref.WriteAsync(stream).GetAwaiter().GetResult();

        var fileId = PdfString.FromBytes(Guid.NewGuid().ToByteArray(), PdfStringSyntax.Hex, ObjectContext.UserCreated);
        var trailer = new Trailer(
            TrailerDictionary.CreateNew(
                4,
                null,
                documentCatalog.Reference,
                null,
                null,
                new ArrayObject([fileId, (PdfString)fileId.Clone()], ObjectContext.UserCreated),
                pdfContext,
                ObjectContext.UserCreated),
            xref.ByteOffset!.Value,
            ObjectContext.UserCreated);

        trailer.WriteAsync(stream).GetAwaiter().GetResult();
        stream.Position = 0;
        return stream;
    }

    private sealed class BootstrapPdfContext : IPdf
    {
        public Stream Data => throw new NotSupportedException();
        public IPdfObjectCollection Objects => throw new NotSupportedException();
        public Task AuthenticateAsync(string password) => throw new NotSupportedException();
        public Task<IList<IndirectObject>> GetAllPagesAsync() => throw new NotSupportedException();
        public Task<Page> GetPageAsync(int pageNumber) => throw new NotSupportedException();
        public Task<int> GetPageCountAsync() => throw new NotSupportedException();
        public Task<Form?> GetFormAsync() => throw new NotSupportedException();
        public Task<PdfMetadata> GetMetadataAsync() => throw new NotSupportedException();
        public Task<Page> AppendPageAsync(Action<PageDictionary.PageCreationOptions>? configureOptions = null) => throw new NotSupportedException();
        public Task<Page> InsertPageAsync(int pageNumber, Action<PageDictionary.PageCreationOptions>? configureOptions = null) => throw new NotSupportedException();
        public Task DeletePageAsync(int pageNumber) => throw new NotSupportedException();
        public Task SetRotationAsync(Rotation rotation) => throw new NotSupportedException();
        public Task<IEnumerable<ExtractedText>> ExtractTextAsync() => throw new NotSupportedException();
        public Task AddWatermarkAsync(string text) => throw new NotSupportedException();
        public void Compress(int dpi, int quality) => throw new NotSupportedException();
        public Task DecompressAsync() => throw new NotSupportedException();
        public Task EncryptAsync(string userPassword, string? ownerPassword = null) => throw new NotSupportedException();
        public Task DecryptAsync(string password) => throw new NotSupportedException();
        public Task AppendPdfAsync(Stream stream) => throw new NotSupportedException();
        public Task SaveAsync(Stream outputStream) => throw new NotSupportedException();
    }
}
