using System.Text;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Elements.Forms;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Text.Encoding.PDFDocEncoding;

namespace ZingPDF;

public class Pdf : IPdf, IDisposable
{
    private readonly Stream _pdfInputStream;

    private Form? _form;

    static Pdf()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding.RegisterProvider(PDFDocEncodingProvider.Instance);
    }

    private Pdf(Stream pdfInputStream)
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));

        _pdfInputStream = pdfInputStream;

        Context = new PdfContext(pdfInputStream);
    }

    public IPdfContext Context { get; }

    public Task<IList<IndirectObject>> GetAllPagesAsync() => Context.Objects.PageTree.GetPagesAsync();

    public async Task<Form?> GetFormAsync()
    {
        var documentCatalog = await Context.Objects.GetDocumentCatalogAsync();

        if (documentCatalog.AcroForm is null)
        {
            return null;
        }

        _form = new Form(documentCatalog.AcroForm, Context);

        return _form;
    }

    public async Task<Page> GetPageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));

        var pageIndirectObject = (await Context.Objects.PageTree.GetPagesAsync())[pageNumber - 1];

        return pageIndirectObject == null
            ? throw new InvalidOperationException()
            : new Page(pageIndirectObject, Context);
    }

    public Task<int> GetPageCountAsync() => Context.Objects.PageTree.GetPageCountAsync();

    public async Task<Page> AppendPageAsync(Action<PageDictionary.PageCreationOptions>? configureOptions = null)
    {
        var pageCreationOptions = PageDictionary.PageCreationOptions.Initialize(configureOptions);

        var rootPageTreeNodeIndirectObject = await Context.Objects.PageTree.GetRootPageTreeNodeAsync();

        DocumentCatalogDictionary documentCatalog = await Context.Objects.GetDocumentCatalogAsync();

        var page = PageDictionary.CreateNew((await documentCatalog.Pages.GetRawValueAsync() as IndirectObjectReference)!, Context, pageCreationOptions);

        var pageIndirectObject = await Context.Objects.AddAsync(page);

        var rootPageTreeNode = (PageTreeNodeDictionary)rootPageTreeNodeIndirectObject.Object;

        // TODO: For now, to simplify adding pages,
        // new pages are appended to the root page tree node.
        // Determine if there's a better way, like ensuring a balanced tree.
        await rootPageTreeNode.AddChildAsync(pageIndirectObject.Reference);

        Context.Objects.Update(rootPageTreeNodeIndirectObject);

        return new Page(pageIndirectObject, Context);
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

        var parentPageTreeNodeIndirectObject = await pageAtNumber.Dictionary.Parent.GetIndirectObjectAsync();
        var parentPageTreeNode = (PageTreeNodeDictionary)parentPageTreeNodeIndirectObject.Object;

        var kidsIndex = (await parentPageTreeNode.Kids.GetAsync()).ToList().IndexOf(pageAtNumber.IndirectObject.Reference);

        // Ensure page has all required properties.
        // required, inheritable properties (Resources, MediaBox) must be set on this or any ancestor
        // TODO: if linearized, required properties may need to be set on all pages. (7.7.3.4 Inheritance of page attributes)
        if (pageCreationOptions.MediaBox is null && (await parentPageTreeNode.MediaBox.GetAsync() == null))
        {
            throw new Exception("This PDF does not have a default page size, you must therefore provide a PageCreationOptions.MediaBox property or ensure an ancestor has a value for this property."); // TODO: proper exception
        }

        var page = PageDictionary.CreateNew(
            parentPageTreeNodeIndirectObject.Reference,
            Context,
            pageCreationOptions
            );

        var newPageIndirectObject = await Context.Objects.AddAsync(page);

        await parentPageTreeNode.AddChildAsync(newPageIndirectObject.Reference);

        await IncrementPageCountAsync(parentPageTreeNode);

        Context.Objects.Update(parentPageTreeNodeIndirectObject);

        return new Page(newPageIndirectObject, Context);
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

        var parentIndirectObject = await page.Dictionary.Parent.GetIndirectObjectAsync()
            ?? throw new InvalidPdfException("Unable to find parent page tree node of requested page");

        var parent = (PageTreeNodeDictionary)parentIndirectObject.Object;

        // TODO: Find pages which are subpages of this, move them so they don't become orphans

        await parent.RemoveChildAsync(page.IndirectObject.Reference);

        await DecrementPageCountAsync(parent);

        Context.Objects.Delete(page.IndirectObject.Id);
        Context.Objects.Update(new IndirectObject(parentIndirectObject.Id, parent));
    }

    public async Task SetRotationAsync(Rotation rotation)
    {
        ArgumentNullException.ThrowIfNull(rotation);

        // Each page may have a rotation property already, therefore a loop is required to set all.
        // i.e. you can't just set an inheritable property on the root page tree node.
        foreach (var page in await Context.Objects.PageTree.GetPagesAsync())
        {
            ((PageDictionary)page.Object).SetRotation(rotation);
            Context.Objects.Update(page);
        }
    }

    public Task<IEnumerable<ExtractedText>> ExtractTextAsync() => new TextExtractor(Context).ExtractTextAsync();

    public void AddWatermark()
    {
        throw new NotImplementedException();
    }

    public void Compress(int dpi, int quality)
    {
        throw new NotImplementedException();
    }

    public async Task DecompressAsync()
    {
        List<IndirectObject> toBeUpdated = [];

        await foreach(var obj in Context.Objects)
        {
            if (obj.Object is IStreamObject streamObj)
            {
                ArrayObject? filterNames = await streamObj.Dictionary.Filter.GetAsync();
                if (filterNames is null || !filterNames.Any())
                {
                    continue;
                }

                // TODO: are there other image types we need to avoid
                // Do not decompress JPEG images.
                if (filterNames.Cast<Name>().Any(x => x.Value == Constants.Filters.DCT))
                {
                    continue;
                }

                var decompressedData = await streamObj.GetDecompressedDataAsync();

                // Must create a new dictionary to hold the stream properties.
                // If we change the values then it could break subsequent decompression of object streams within this loop.
                // (Currently decompressed object streams are not cached in the PdfObjectManager)
                var newStreamDict = StreamDictionary.FromDictionary(streamObj.Dictionary);

                newStreamDict.Unset(Constants.DictionaryKeys.Stream.Filter);
                newStreamDict.Unset(Constants.DictionaryKeys.Stream.DecodeParms);
                newStreamDict.Set<Number>(Constants.DictionaryKeys.Stream.Length, decompressedData.Length);
                newStreamDict.Set<Number>(Constants.DictionaryKeys.Stream.DL, decompressedData.Length);

                if (streamObj.Dictionary.Type != null)
                {
                    newStreamDict.Set(Constants.DictionaryKeys.Type, streamObj.Dictionary.Type);
                }

                var newObj = new StreamObject<IStreamDictionary>(
                    decompressedData,
                    newStreamDict
                );

                toBeUpdated.Add(new IndirectObject(obj.Id, newObj));
            }
        }

        foreach (var io in toBeUpdated)
        {
            Context.Objects.Update(io);
        }
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
        await new PdfMerger(this, Load(stream)).AppendAsync();
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

        var incrementalUpdate = await Context.Objects.GenerateUpdateDeltaAsync(Context);
        if (incrementalUpdate != null)
        {
            await incrementalUpdate.WriteAsync(outputStream);
        }

        await outputStream.FlushAsync();

        Dispose();
    }

    public static Pdf Load(Stream pdfInputStream)
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));

        if (!pdfInputStream.CanSeek)
        {
            throw new ArgumentException("Provided stream must be seekable");
        }

        return new Pdf(pdfInputStream);
    }

    // TODO: move to testable class?
    /// <summary>
    /// Recursively increment the page count of this page tree node and all its ancestors
    /// </summary>
    private async Task IncrementPageCountAsync(PageTreeNodeDictionary pageTreeNode)
    {
        PageTreeNodeDictionary? parentPageTreeNode = await pageTreeNode.Parent.GetAsync();
        if (parentPageTreeNode is null)
        {
            return;
        }

        var parentPageTreeNodeIndirectObject = await pageTreeNode.Parent.GetIndirectObjectAsync();

        await parentPageTreeNode.IncrementCountAsync();

        Context.Objects.Update(parentPageTreeNodeIndirectObject);

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

        var parentPageTreeNodeIndirectObject = await pageTreeNode.Parent.GetIndirectObjectAsync();
        var parentPageTreeNode = (PageTreeNodeDictionary)parentPageTreeNodeIndirectObject.Object;

        await parentPageTreeNode.DecrementCountAsync();

        Context.Objects.Update(parentPageTreeNodeIndirectObject);

        await DecrementPageCountAsync(parentPageTreeNode);
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
