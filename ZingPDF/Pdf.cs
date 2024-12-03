using ZingPDF.Elements;
using ZingPDF.Elements.Forms;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Linearization;
using ZingPDF.Parsing;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

/// <summary>
/// Represents an editable PDF.
/// </summary>
/// <remarks>
/// This class is disposable. The underlying <see cref="Stream"/> will remain open until the instance is disposed.<para></para>
/// </remarks>
public class Pdf : BasePdf, IEditablePdf
{
    /// <summary>
    /// Internal constructor for creating a <see cref="Pdf"/> instance from its constituent parts.
    /// </summary>
    internal Pdf(
        Stream pdfInputStream,
        DocumentCatalogDictionary documentCatalog,
        Trailer? trailer,
        IndirectObject? xrefStream,
        IndirectObjectManager indirectObjectManager,
        LinearizationParameterDictionary? linearizationDictionary
        )
        : base(pdfInputStream, documentCatalog, trailer, xrefStream, indirectObjectManager, linearizationDictionary)
    {
    }

    public IndirectObjectManager IndirectObjectManager => (IndirectObjectManager)IndirectObjects;

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
        if (outputStream.Length == 0)
        {
            _pdfInputStream.Position = 0;
            await _pdfInputStream.CopyToAsync(outputStream);
        }

        _form?.UpdateAsync();

        await new IncrementalUpdate(this, IndirectObjectManager).WriteAsync(outputStream);

        await outputStream.FlushAsync();
    }

    public async Task<Page> AppendPageAsync(Action<PageDictionary.PageCreationOptions>? configureOptions = null)
    {
        var pageCreationOptions = PageDictionary.PageCreationOptions.Initialize(configureOptions);

        var rootPageTreeNodeIndirectObject = await PageTree.GetRootPageTreeNodeAsync();

        var page = PageDictionary.CreateNew(DocumentCatalog.Pages, pageCreationOptions);

        var pageIndirectObject = IndirectObjectManager.Add(page);

        var rootPageTreeNode = (PageTreeNodeDictionary)rootPageTreeNodeIndirectObject.Object;

        // TODO: For now, to simplify adding pages,
        // new pages are appended to the root page tree node.
        // Determine if there's a better way, like ensuring a balanced tree.
        rootPageTreeNode.AddChild(pageIndirectObject.Id.Reference);

        IndirectObjectManager.Update(rootPageTreeNodeIndirectObject);

        return new Page(pageIndirectObject, IndirectObjectManager);
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

        IndirectObjectManager.Delete(page.IndirectObject.Id);
        IndirectObjectManager.Update(new IndirectObject(parentIndirectObject.Id, parent));
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

        var parentPageTreeNodeIndirectObject = await IndirectObjects.GetAsync(pageAtNumber.Dictionary.Parent);
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

        var newPageIndirectObject = IndirectObjectManager.Add(page);

        parentPageTreeNode.AddChild(newPageIndirectObject.Id.Reference);

        await IncrementPageCountAsync(parentPageTreeNode);

        IndirectObjectManager.Update(parentPageTreeNodeIndirectObject);

        return new Page(newPageIndirectObject, IndirectObjectManager);
    }

    public async Task SetRotationAsync(Rotation rotation)
    {
        ArgumentNullException.ThrowIfNull(rotation);

        // Each page may have a rotation property already, therefore a loop is required to set all.
        // i.e. you can't just set an inheritable property on the root page tree node.
        foreach (var page in await PageTree.GetPagesAsync())
        {
            ((PageDictionary)page.Object).SetRotation(rotation);
            IndirectObjectManager.Update(page);
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
        await new PdfMerger(this, await PdfParser.OpenReadOnlyAsync(stream)).AppendAsync();
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

        var parentPageTreeNodeIndirectObject = await IndirectObjects.GetAsync(pageTreeNode.Parent);
        var parentPageTreeNode = (PageTreeNodeDictionary)parentPageTreeNodeIndirectObject.Object;

        parentPageTreeNode.IncrementCount();

        IndirectObjectManager.Update(parentPageTreeNodeIndirectObject);

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

        var parentPageTreeNodeIndirectObject = await IndirectObjects.GetAsync(pageTreeNode.Parent);
        var parentPageTreeNode = (PageTreeNodeDictionary)parentPageTreeNodeIndirectObject.Object;

        parentPageTreeNode.DecrementCount();

        IndirectObjectManager.Update(parentPageTreeNodeIndirectObject);

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

        var parent = await IndirectObjectManager.GetAsync<PageTreeNodeDictionary>(parentPageTreeNode.Parent);
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
