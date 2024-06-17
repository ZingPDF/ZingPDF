using ZingPDF.Drawing;
using ZingPDF.ObjectModel.CommonDataStructures;
using ZingPDF.ObjectModel.DocumentStructure.PageTree;
using ZingPDF.Parsing.IncrementalUpdates;

namespace ZingPDF.Parsing;

public class Pdf : IEditablePdf
{
    private readonly ReadOnlyPdf _sourcePdf;
    private readonly IndirectObjectManager _indirectObjectManager;

    /// <summary>
    /// Private constructor for creating a <see cref="Pdf"/> instance from a <see cref="ReadOnlyPdf"/>.
    /// </summary>
    private Pdf(ReadOnlyPdf sourcePdf)
    {
        _sourcePdf = sourcePdf ?? throw new ArgumentNullException(nameof(sourcePdf));
        _indirectObjectManager = new IndirectObjectManager(_sourcePdf.IndirectObjects);
    }

    /// <summary>
    /// Open a PDF from a stream.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> which contains the PDF data.</param>
    /// <returns>A <see cref="Pdf"/> instance.</returns>
    /// <example>
    /// <![CDATA[
    /// using var inputFileStream = new FileStream("example.pdf", FileMode.Open);
    /// 
    /// var pdf = await EditablePdf.OpenAsync(inputFileStream);
    /// ]]>
    /// </example>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<Pdf> OpenAsync(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable", nameof(stream));
        }

        return new(await ReadOnlyPdf.OpenAsync(stream));
    }

    public void AddWatermark()
    {
        throw new NotImplementedException();
    }

    public async Task AppendPageAsync(Page.PageCreationOptions? pageCreationOptions)
    {
        pageCreationOptions ??= Page.PageCreationOptions.Default;

        var page = Page.CreateNew(_sourcePdf.DocumentCatalog.Pages.Id.Reference, pageCreationOptions);

        var pageIndirectObject = _indirectObjectManager.Add(page);

        var rootPageTreeNodeIndirectObject = await _indirectObjectManager.GetAsync(_sourcePdf.DocumentCatalog.Pages);
        var rootPageTreeNode = rootPageTreeNodeIndirectObject!.Get<PageTreeNode>();

        // TODO: For now, to simplify adding pages,
        // new pages are appended to the root page tree node.
        // Determine if there's a better way, like ensuring a balanced tree.
        rootPageTreeNode.Kids.Add(pageIndirectObject.Id.Reference);

        rootPageTreeNode.PageCount++;

        _indirectObjectManager.Update(rootPageTreeNodeIndirectObject);
    }

    public void AppendPdf(Stream stream)
    {
        throw new NotImplementedException();
    }

    public void CompleteForm(IDictionary<string, string> values)
    {
        throw new NotImplementedException();
    }

    public void Compress(int dpi, int quality)
    {
        throw new NotImplementedException();
    }

    public void Decrypt()
    {
        throw new NotImplementedException();
    }

    public Task DeletePageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

        // TODO: check if there's a more efficient way to do this.
        var pages = await _pdfNavigator.GetPagesAsync();

        var pageIndirectObject = pages.ElementAt(pageNumber - 1);
        var page = (pageIndirectObject.Children.First() as Page)!;
        var parentIndirectObject = await _pdfNavigator.DereferenceIndirectObjectAsync(page.Parent);
        var parent = (parentIndirectObject.Children.First() as PageTreeNode)!;

        parent.Kids = parent.Kids.Cast<IndirectObjectReference>().Where(x => x.Id != pageIndirectObject.Id).ToArray();
        parent.PageCount--;

        _pdfNavigator.DeleteObject(pageIndirectObject.Id);
        _pdfNavigator.UpdateObject(new IndirectObject(parentIndirectObject.Id, parent));
    }

    public void Draw(int pageNumber, IEnumerable<Drawing.Path> paths, IEnumerable<Text> text, IEnumerable<Image> imageOperations, CoordinateSystem coordinateSystem = CoordinateSystem.BottomUp)
    {
        throw new NotImplementedException();
    }

    public void Encrypt()
    {
        throw new NotImplementedException();
    }

    public IDictionary<string, string?> GetFields()
    {
        throw new NotImplementedException();
    }

    public Task<Page> GetPageAsync(int pageNumber)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetPageCountAsync()
    {
        throw new NotImplementedException();
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

        // TODO: using PageTree may be wrong if a page has been updated. Does it matter since this is only to get the parent?
        var pageAtNumberIndirectObject = await _sourcePdf.PageTree.GetAsync(pageNumber);
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

        var newKids = parentPageTreeNode.Kids.ToList();
        newKids.Insert(kidsIndex, newPageIndirectObject.Id.Reference);

        parentPageTreeNode.Kids = newKids.ToArray();
        parentPageTreeNode.PageCount++;

        await IncrementPageCountAsync(parentPageTreeNode);

        _indirectObjectManager.Update(parentPageTreeNodeIndirectObject);
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
            await _sourcePdf.CopyToAsync(outputStream);
        }

        await new IncrementalUpdate(_sourcePdf, _indirectObjectManager).WriteAsync(outputStream);

        await outputStream.FlushAsync();
    }

    public Task SetPageRotationAsync(int pageNumber, Rotation rotation)
    {
        throw new NotImplementedException();
    }

    public void Sign()
    {
        throw new NotImplementedException();
    }

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

        parentPageTreeNode.PageCount++;

        _indirectObjectManager.Update(parentPageTreeNodeIndirectObject);

        await IncrementPageCountAsync(parentPageTreeNode);
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
