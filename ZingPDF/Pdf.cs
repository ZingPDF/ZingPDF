using System.Runtime.CompilerServices;
using ZingPDF.Elements;
using ZingPDF.Elements.Forms;
using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Parsing;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

// TODO: consider inheriting from ReadOnlyPdf
// Actually no, this IS NOT a ReadOnlyPDF. There should perhaps be a base type or interface for Pdf and ReadOnlyPdf
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

        PageTree = new PageTree(_indirectObjectManager, DocumentCatalog.Pages);
    }

    #region IPdf

    public IIndirectObjectDictionary IndirectObjects => _indirectObjectManager;
    public Trailer? Trailer => _sourcePdf.Trailer;
    public IndirectObject? CrossReferenceStream => _sourcePdf.CrossReferenceStream;
    public DocumentCatalogDictionary DocumentCatalog => _sourcePdf.DocumentCatalog;
    public PageTree PageTree { get; }

    public ITrailerDictionary TrailerDictionary => _sourcePdf.TrailerDictionary;

    // TODO: logic is duplicated in readonlypdf. Consider sharing.
    public async Task<Page> GetPageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));

        var pageIndirectObject = (await PageTree.GetPagesAsync())[pageNumber - 1];

        return pageIndirectObject == null
            ? throw new InvalidOperationException()
            : new Page(pageIndirectObject, _indirectObjectManager);
    }

    Task<IList<IndirectObject>> IPdf.GetAllPagesAsync() => PageTree.GetPagesAsync();

    public Task<int> GetPageCountAsync() => PageTree.GetPageCountAsync();

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

    public async Task<Page> AppendPageAsync(Action<PageDictionary.PageCreationOptions>? configureOptions = null)
    {
        var pageCreationOptions = PageDictionary.PageCreationOptions.Initialize(configureOptions);

        var rootPageTreeNodeIndirectObject = await IndirectObjects.GetAsync(DocumentCatalog.Pages)
            ?? throw new InvalidPdfException("Unable to find root page tree node");

        var page = PageDictionary.CreateNew(DocumentCatalog.Pages, pageCreationOptions);

        var pageIndirectObject = _indirectObjectManager.Add(page);

        var rootPageTreeNode = (PageTreeNodeDictionary)rootPageTreeNodeIndirectObject.Object;

        // TODO: For now, to simplify adding pages,
        // new pages are appended to the root page tree node.
        // Determine if there's a better way, like ensuring a balanced tree.
        rootPageTreeNode.AddChild(pageIndirectObject.Id.Reference);

        _indirectObjectManager.Update(rootPageTreeNodeIndirectObject);

        return new Page(pageIndirectObject, _indirectObjectManager);
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

        var parentIndirectObject = await IndirectObjects.GetAsync(page.Dictionary.Parent)
            ?? throw new InvalidPdfException("Unable to find parent page tree node of requested page");

        var parent = (PageTreeNodeDictionary)parentIndirectObject.Object;

        // TODO: Find pages which are subpages of this, move them so they don't become orphans

        parent.RemoveChild(page.IndirectObject.Id.Reference);

        await DecrementPageCountAsync(parent);

        _indirectObjectManager.Delete(page.IndirectObject.Id);
        _indirectObjectManager.Update(new IndirectObject(parentIndirectObject.Id, parent));
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

        var parentPageTreeNodeIndirectObject = await _indirectObjectManager.GetAsync(pageAtNumber.Dictionary.Parent);
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

        var newPageIndirectObject = _indirectObjectManager.Add(page);

        parentPageTreeNode.AddChild(newPageIndirectObject.Id.Reference);

        await IncrementPageCountAsync(parentPageTreeNode);

        _indirectObjectManager.Update(parentPageTreeNodeIndirectObject);

        return new Page(newPageIndirectObject, _indirectObjectManager);
    }

    public async Task SetRotationAsync(Rotation rotation)
    {
        ArgumentNullException.ThrowIfNull(rotation);

        // Each page may have a rotation property already, therefore a loop is required to set all.
        // i.e. you can't just set an inheritable property on the root page tree node.
        foreach (var page in await PageTree.GetPagesAsync())
        {
            ((PageDictionary)page.Object).SetRotation(rotation);
            _indirectObjectManager.Update(page);
        }
    }

    // TODO: duplicate logic in ReadOnlyPdf. See if we can share it.
    public Form? GetForm()
    {
        if (DocumentCatalog.AcroForm is null)
        {
            return null;
        }

        return new Form(DocumentCatalog.AcroForm, IndirectObjects);
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
        // Keeping this simple for now...
        // 1. Parse supplied PDF
        // 2. Append all pages to this PDF
        // 3. Copy all referenced page resources

        var sourcePdf = await PdfParser.OpenReadOnlyAsync(stream);

        List<IndirectObject> pagesToAdd = [];
        //HashSet<IndirectObject> objectsToAdd = [];

        // Simple document merging...
        // - We're going to visit every page and page tree node in the new PDF
        // - All pages we find will be added to this document
        // - Along the way we'll build a resource dictionary from all encountered nodes

        var mergedResourceDictionary = new Dictionary<Name, IPdfObject>();
        var nodes = await sourcePdf.PageTree.GetAllNodesAsync();

        foreach (var node in nodes)
        {
            var resources = ((Dictionary?)node.Object)?.Get<Dictionary>(Constants.DictionaryKeys.Page.Resources);

            if (resources != null)
            {
                mergedResourceDictionary = resources?.MergeInto(mergedResourceDictionary!);
            }

            if (node.Object is PageDictionary)
            {
                pagesToAdd.Add(node);
            }
        }

        _indirectObjectManager.AddRange(pagesToAdd);
        
        // TODO: add merged resource dictionary
        // TODO: add referenced resource objects
    }

    //// Returning resources as indirect objects allows us to de-dupe them by ID.
    //private IEnumerable<IndirectObject> FindResources(PageDictionary page)
    //{
    //    // 1. Page resources
    //    if (page.Resources is not null)
    //    {
    //        // Don't bother considering property inheritance as 
    //    }
    //}

    #endregion

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

        var parentPageTreeNodeIndirectObject = await _indirectObjectManager.GetAsync(pageTreeNode.Parent);
        var parentPageTreeNode = (PageTreeNodeDictionary)parentPageTreeNodeIndirectObject.Object;

        parentPageTreeNode.IncrementCount();

        _indirectObjectManager.Update(parentPageTreeNodeIndirectObject);

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

        var parentPageTreeNodeIndirectObject = await _indirectObjectManager.GetAsync(pageTreeNode.Parent);
        var parentPageTreeNode = (PageTreeNodeDictionary)parentPageTreeNodeIndirectObject.Object;

        parentPageTreeNode.DecrementCount();

        _indirectObjectManager.Update(parentPageTreeNodeIndirectObject);

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

        var parent = await _indirectObjectManager.GetAsync<PageTreeNodeDictionary>(parentPageTreeNode.Parent);
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
