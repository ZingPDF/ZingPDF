using ZingPDF.Elements;
using ZingPDF.Elements.Forms;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Parsing.Parsers.FileStructure;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

public class Pdf : IPdf, IDisposable
{
    private readonly Stream _pdfInputStream;
    private readonly DocumentCatalogDictionary _documentCatalog;

    private Form? _form;

    private Pdf(
        Stream pdfInputStream,
        DocumentCatalogDictionary documentCatalog,
        PdfObjectManager pdfObjectManager
        )
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));
        ArgumentNullException.ThrowIfNull(documentCatalog, nameof(documentCatalog));
        ArgumentNullException.ThrowIfNull(pdfObjectManager, nameof(pdfObjectManager));

        _pdfInputStream = pdfInputStream;
        _documentCatalog = documentCatalog;

        PageTree = new PageTree(pdfObjectManager, (documentCatalog.Pages.Value as IndirectObjectReference)!);

        IndirectObjects = pdfObjectManager;
    }

    public PdfObjectManager IndirectObjects { get; }

    public PageTree PageTree { get; }

    public Task<IList<IndirectObject>> GetAllPagesAsync() => PageTree.GetPagesAsync();

    public Form? GetForm()
    {
        if (_documentCatalog.AcroForm is null)
        {
            return null;
        }

        _form = new Form((_documentCatalog.AcroForm.Value as IndirectObjectReference)!, IndirectObjects);

        return _form;
    }

    public async Task<Page> GetPageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));

        var pageIndirectObject = (await PageTree.GetPagesAsync())[pageNumber - 1];

        return pageIndirectObject == null
            ? throw new InvalidOperationException()
            : new Page(pageIndirectObject, IndirectObjects);
    }

    public Task<int> GetPageCountAsync() => PageTree.GetPageCountAsync();

    public async Task<Page> AppendPageAsync(Action<PageDictionary.PageCreationOptions>? configureOptions = null)
    {
        var pageCreationOptions = PageDictionary.PageCreationOptions.Initialize(configureOptions);

        var rootPageTreeNodeIndirectObject = await PageTree.GetRootPageTreeNodeAsync();

        var page = PageDictionary.CreateNew((_documentCatalog.Pages.Value as IndirectObjectReference)!, pageCreationOptions);

        var pageIndirectObject = IndirectObjects.Add(page);

        var rootPageTreeNode = (PageTreeNodeDictionary)rootPageTreeNodeIndirectObject.Object;

        // TODO: For now, to simplify adding pages,
        // new pages are appended to the root page tree node.
        // Determine if there's a better way, like ensuring a balanced tree.
        rootPageTreeNode.AddChild(pageIndirectObject.Id.Reference);

        IndirectObjects.Update(rootPageTreeNodeIndirectObject);

        return new Page(pageIndirectObject, IndirectObjects);
    }

    public async Task<Page> InsertPageAsync(int pageNumber, Action<PageDictionary.PageCreationOptions>? configureOptions = null)
    {
        // get page at number
        // get parent page tree node
        // add new page indirect object
        // add new page ref to kids property
        // increment page count
        // - this involves recursively updating multiple nodes in page tree

        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

        var pageCreationOptions = PageDictionary.PageCreationOptions.Initialize(configureOptions);

        var count = await GetPageCountAsync();

        if (pageNumber > count)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} must be less than or equal to the total number of pages. To add a page to the end of the PDF, use {nameof(AppendPageAsync)}");
        }

        var pageAtNumber = await GetPageAsync(pageNumber);

        var parentPageTreeNodeIndirectObject = await pageAtNumber.Dictionary.Parent.GetIndirectObjectAsync(IndirectObjects);
        var parentPageTreeNode = (PageTreeNodeDictionary)parentPageTreeNodeIndirectObject.Object;

        var kidsIndex = parentPageTreeNode.Kids.ToList().IndexOf(pageAtNumber.IndirectObject.Id.Reference);

        // Ensure page has all required properties.
        // required, inheritable properties (Resources, MediaBox) must be set on this or any ancestor
        // TODO: if linearized, required properties may need to be set on all pages. (7.7.3.4 Inheritance of page attributes)
        if (pageCreationOptions.MediaBox is null && !await AncestorHasMediaBox(parentPageTreeNode))
        {
            throw new Exception("This PDF does not have a default page size, you must therefore provide a PageCreationOptions.MediaBox property or ensure an ancestor has a value for this property."); // TODO: proper exception
        }

        var page = PageDictionary.CreateNew(
            parentPageTreeNodeIndirectObject.Id.Reference,
            pageCreationOptions
            );

        var newPageIndirectObject = IndirectObjects.Add(page);

        parentPageTreeNode.AddChild(newPageIndirectObject.Id.Reference);

        await IncrementPageCountAsync(parentPageTreeNode);

        IndirectObjects.Update(parentPageTreeNodeIndirectObject);

        return new Page(newPageIndirectObject, IndirectObjects);
    }

    public async Task DeletePageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

        var count = await GetPageCountAsync();

        if (pageNumber > count)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} must be less than or equal to the total number of pages.");
        }

        var page = await GetPageAsync(pageNumber);

        var parentIndirectObject = await page.Dictionary.Parent.GetIndirectObjectAsync(IndirectObjects)
            ?? throw new InvalidPdfException("Unable to find parent page tree node of requested page");

        var parent = (PageTreeNodeDictionary)parentIndirectObject.Object;

        // TODO: Find pages which are subpages of this, move them so they don't become orphans

        parent.RemoveChild(page.IndirectObject.Id.Reference);

        await DecrementPageCountAsync(parent);

        IndirectObjects.Delete(page.IndirectObject.Id);
        IndirectObjects.Update(new IndirectObject(parentIndirectObject.Id, parent));
    }

    public async Task SetRotationAsync(Rotation rotation)
    {
        ArgumentNullException.ThrowIfNull(rotation);

        // Each page may have a rotation property already, therefore a loop is required to set all.
        // i.e. you can't just set an inheritable property on the root page tree node.
        foreach (var page in await PageTree.GetPagesAsync())
        {
            ((PageDictionary)page.Object).SetRotation(rotation);
            IndirectObjects.Update(page);
        }
    }

    public void AddWatermark()
    {
        throw new NotImplementedException();
    }

    public void Compress(int dpi, int quality)
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

    public async Task AppendPdfAsync(Stream stream)
    {
        await new PdfMerger(this, await LoadAsync(stream)).AppendAsync();
    }

    public async Task SaveAsync(Stream outputStream, PdfSaveOptions? saveOptions = null)
    {
        ArgumentNullException.ThrowIfNull(outputStream);
        if (!outputStream.CanWrite) throw new ArgumentException("Provided output stream must be writable", nameof(outputStream));

        saveOptions ??= PdfSaveOptions.Default;

        // Copy original PDf to output if required.
        if (outputStream.Length == 0)
        {
            _pdfInputStream.Position = 0;
            await _pdfInputStream.CopyToAsync(outputStream);
        }

        _form?.UpdateAsync();

        var incrementalUpdate = await IndirectObjects.GenerateUpdateDeltaAsync();
        if (incrementalUpdate != null)
        {
            await incrementalUpdate.WriteAsync(outputStream);
        }

        await outputStream.FlushAsync();

        Dispose();
    }

    public static async Task<Pdf> LoadAsync(Stream pdfInputStream)
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));

        if (!pdfInputStream.CanSeek)
        {
            throw new ArgumentException("Provided stream must be seekable");
        }

        var documentVersions = await DocumentVersionParser.ParseDocumentVersionsAsync(pdfInputStream);

        var pdfObjectManager = new PdfObjectManager(documentVersions);
        var catalog = await pdfObjectManager.GetDocumentCatalogAsync();

        return new Pdf(pdfInputStream, catalog, pdfObjectManager);
    }

    // TODO: move to testable class?
    /// <summary>
    /// Recursively increment the page count of this page tree node and all its ancestors
    /// </summary>
    private async Task IncrementPageCountAsync(PageTreeNodeDictionary pageTreeNode)
    {
        if (pageTreeNode.Parent is null)
        {
            return;
        }

        var parentPageTreeNodeIndirectObject = await pageTreeNode.Parent.GetIndirectObjectAsync(IndirectObjects);
        var parentPageTreeNode = (PageTreeNodeDictionary)parentPageTreeNodeIndirectObject.Object;

        parentPageTreeNode.IncrementCount();

        IndirectObjects.Update(parentPageTreeNodeIndirectObject);

        await IncrementPageCountAsync(parentPageTreeNode);
    }

    // TODO: move to testable class?
    /// <summary>
    /// Recursively decrement the page count of this page tree node and all its ancestors
    /// </summary>
    private async Task DecrementPageCountAsync(PageTreeNodeDictionary pageTreeNode)
    {
        if (pageTreeNode.Parent is null)
        {
            return;
        }

        var parentPageTreeNodeIndirectObject = await pageTreeNode.Parent.GetIndirectObjectAsync(IndirectObjects);
        var parentPageTreeNode = (PageTreeNodeDictionary)parentPageTreeNodeIndirectObject.Object;

        parentPageTreeNode.DecrementCount();

        IndirectObjects.Update(parentPageTreeNodeIndirectObject);

        await DecrementPageCountAsync(parentPageTreeNode);
    }

    // TODO: move to testable class?
    /// <summary>
    /// Recursively walk up the page tree to check for the presence of a MediaBox property.
    /// </summary>
    private async Task<bool> AncestorHasMediaBox(PageTreeNodeDictionary parentPageTreeNode)
    {
        if (parentPageTreeNode.MediaBox is not null)
        {
            return true;
        }

        if (parentPageTreeNode.Parent is null)
        {
            return false;
        }

        var parent = await parentPageTreeNode.Parent.GetAsync(IndirectObjects);
        if (parent == null)
        {
            return false;
        }

        if (await AncestorHasMediaBox(parent))
        {
            return true;
        }

        return false;
    }

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ((IDisposable)_pdfInputStream).Dispose();
        }
    }

    #endregion
}
