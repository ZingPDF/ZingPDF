using ZingPDF.Drawing;
using ZingPDF.IncrementalUpdates;
using ZingPDF.ObjectModel.CommonDataStructures;
using ZingPDF.ObjectModel.DocumentStructure;
using ZingPDF.ObjectModel.DocumentStructure.PageTree;
using ZingPDF.ObjectModel.FileStructure.Trailer;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF;

public class Pdf : IEditablePdf
{
    private readonly IPdf _sourcePdf;
    private readonly IndirectObjectManager _indirectObjectManager;

    /// <summary>
    /// Internal constructor for creating a <see cref="Pdf"/> instance from an <see cref="IPdf"/>.
    /// </summary>
    internal Pdf(IPdf sourcePdf)
    {
        _sourcePdf = sourcePdf ?? throw new ArgumentNullException(nameof(sourcePdf));
        _indirectObjectManager = new IndirectObjectManager(sourcePdf.IndirectObjects);
    }

    #region IPdf

    public IIndirectObjectDictionary IndirectObjects => _indirectObjectManager;
    public Trailer? Trailer => _sourcePdf.Trailer;
    public ITrailerDictionary TrailerDictionary => _sourcePdf.TrailerDictionary;
    public DocumentCatalogDictionary DocumentCatalog => _sourcePdf.DocumentCatalog;

    public Task<IndirectObject> GetPageAsync(int pageNumber)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetPageCountAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Save the PDF to the provided output stream.
    /// </summary>
    /// <remarks>
    /// If the PDF has been modified, this method will apply all updates as an incremental update to the PDF, thereby preserving file history. <para></para>
    /// </remarks>
    /// <param name="outputStream">The <see cref="Stream"/> to which to write the PDF.</param>
    /// <param name="saveOptions">PDF save options.</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task SaveAsync(Stream outputStream, PdfSaveOptions? saveOptions = null)
    {
        ArgumentNullException.ThrowIfNull(outputStream);
        if (!outputStream.CanWrite) throw new ArgumentException("Provided output stream must be writable", nameof(outputStream));

        saveOptions ??= PdfSaveOptions.Default;

        // Copy original PDf to output if required.
        // TODO: make sure this behaviour is well documented.
        if (outputStream.Length == 0)
        {
            await _sourcePdf.SaveAsync(outputStream, saveOptions);
        }

        await new IncrementalUpdate(_sourcePdf, _indirectObjectManager).WriteAsync(outputStream);

        await outputStream.FlushAsync();
    }

    #endregion

    #region IEditablePdf

    public async Task AppendPageAsync(Page.PageCreationOptions? pageCreationOptions)
    {
        pageCreationOptions ??= Page.PageCreationOptions.Default;

        var rootPageTreeNodeIndirectObject = await IndirectObjects.GetAsync(DocumentCatalog.Pages)
            ?? throw new InvalidPdfException("Unable to find root page tree node");

        var page = Page.CreateNew(DocumentCatalog.Pages, pageCreationOptions);

        var pageIndirectObject = _indirectObjectManager.Add(page);

        var rootPageTreeNode = rootPageTreeNodeIndirectObject!.Get<PageTreeNode>();

        // TODO: For now, to simplify adding pages,
        // new pages are appended to the root page tree node.
        // Determine if there's a better way, like ensuring a balanced tree.
        rootPageTreeNode.AddChild(pageIndirectObject.Id.Reference);

        _indirectObjectManager.Update(rootPageTreeNodeIndirectObject);
    }

    public async Task DeletePageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

        var count = await GetPageCountAsync();

        if (pageNumber > count)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} must be less than or equal to the total number of pages.");
        }

        var pageIndirectObject = await GetPageAsync(pageNumber);
        var page = pageIndirectObject.Get<Page>();

        var parentIndirectObject = await IndirectObjects.GetAsync(page.Parent)
            ?? throw new InvalidPdfException("Unable to find parent page tree node of requested page");

        var parent = (parentIndirectObject.Children.First() as PageTreeNode)!;

        // TODO: Find pages which are subpages of this, move them so they don't become orphans

        parent.RemoveChild(pageIndirectObject.Id.Reference);

        // TODO: decrement page count in ancestors?

        _indirectObjectManager.Delete(pageIndirectObject.Id);
        _indirectObjectManager.Update(new IndirectObject(parentIndirectObject.Id, parent));
    }

    public async Task InsertPageAsync(int pageNumber, Page.PageCreationOptions? pageCreationOptions)
    {
        // get page at number
        // get parent page tree node
        // add new page indirect object
        // add new page ref to kids property
        // increment page count
        // - this involves recursively updating multiple nodes in page tree

        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

        pageCreationOptions ??= Page.PageCreationOptions.Default;

        var count = await GetPageCountAsync();

        if (pageNumber > count)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} must be less than or equal to the total number of pages. To add a page to the end of the PDF, use {nameof(AppendPageAsync)}");
        }

        var pageAtNumberIndirectObject = await GetPageAsync(pageNumber);
        var pageAtNumber = pageAtNumberIndirectObject.Get<Page>();
        var parentPageTreeNodeIndirectObject = await _indirectObjectManager.GetAsync(pageAtNumber.Parent);
        var parentPageTreeNode = parentPageTreeNodeIndirectObject!.Get<PageTreeNode>();

        var kidsIndex = parentPageTreeNode.Kids.ToList().IndexOf(pageAtNumberIndirectObject.Id.Reference);

        // Ensure page has all required properties.
        // required, inheritable properties (Resources, MediaBox) must be set on this or any ancestor
        // TODO: if linearized, required properties may need to be set on all pages. (7.7.3.4 Inheritance of page attributes)
        if (pageCreationOptions.MediaBox is null && !await AncestorHasMediaBox(parentPageTreeNode))
        {
            throw new Exception("This PDF does not have a default page size, you must therefore provide a PageCreationOptions.MediaBox property or ensure an ancestor has a value for this property."); // TODO: proper exception
        }

        var page = Page.CreateNew(
            parentPageTreeNodeIndirectObject.Id.Reference,
            pageCreationOptions
            );

        var newPageIndirectObject = _indirectObjectManager.Add(page);

        parentPageTreeNode.AddChild(newPageIndirectObject.Id.Reference);

        await IncrementPageCountAsync(parentPageTreeNode);

        _indirectObjectManager.Update(parentPageTreeNodeIndirectObject);
    }

    public Task SetPageRotationAsync(int pageNumber, Rotation rotation)
    {
        //        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        //        ArgumentNullException.ThrowIfNull(rotation);

        //        // TODO: check if there's a more efficient way to do this.
        //        var pages = await _pdfNavigator.GetPagesAsync();

        //        var page = pages.ElementAt(pageNumber - 1);

        //        (page.Children.First() as Page)!.Rotate = rotation;

        //        _pdfNavigator.UpdateObject(page);

        throw new NotImplementedException();
    }

    public void Draw(int pageNumber, IEnumerable<Drawing.Path> paths, IEnumerable<Text> text, IEnumerable<Image> imageOperations, CoordinateSystem coordinateSystem = CoordinateSystem.BottomUp)
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

    public void AppendPdf(Stream stream)
    {
        throw new NotImplementedException();
    }

    #endregion

    // TODO: move to testable class?
    /// <summary>
    /// Recursively increment the page count of this page tree node and all its ancestors
    /// </summary>
    private async Task IncrementPageCountAsync(PageTreeNode pageTreeNode)
    {
        if (pageTreeNode.Parent is null)
        {
            return;
        }

        var parentPageTreeNodeIndirectObject = await _indirectObjectManager.GetAsync(pageTreeNode.Parent);
        var parentPageTreeNode = parentPageTreeNodeIndirectObject!.Get<PageTreeNode>();

        parentPageTreeNode.IncrementCount();

        _indirectObjectManager.Update(parentPageTreeNodeIndirectObject);

        await IncrementPageCountAsync(parentPageTreeNode);
    }

    // TODO: move to testable class?
    /// <summary>
    /// Recursively decrement the page count of this page tree node and all its ancestors
    /// </summary>
    private async Task DecrementPageCountAsync(PageTreeNode pageTreeNode)
    {
        if (pageTreeNode.Parent is null)
        {
            return;
        }

        var parentPageTreeNodeIndirectObject = await _indirectObjectManager.GetAsync(pageTreeNode.Parent);
        var parentPageTreeNode = parentPageTreeNodeIndirectObject!.Get<PageTreeNode>();

        parentPageTreeNode.DecrementCount();

        _indirectObjectManager.Update(parentPageTreeNodeIndirectObject);

        await DecrementPageCountAsync(parentPageTreeNode);
    }

    // TODO: move to testable class?
    /// <summary>
    /// Recursively walk up the page tree to check for the presence of a MediaBox property.
    /// </summary>
    private async Task<bool> AncestorHasMediaBox(PageTreeNode parentPageTreeNode)
    {
        if (parentPageTreeNode.MediaBox is not null)
        {
            return true;
        }

        if (parentPageTreeNode.Parent is null)
        {
            return false;
        }

        var parent = await _indirectObjectManager.GetAsync<PageTreeNode>(parentPageTreeNode.Parent);
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
}
