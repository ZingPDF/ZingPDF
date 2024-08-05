using Nito.AsyncEx;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Extensions;
using ZingPDF.Graphics;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Text;
using ZingPDF.Text.SimpleFonts;

namespace ZingPDF;

// TODO: consider inheriting from ReadOnlyPdf
public class Pdf : IEditablePdf
{
    private readonly IPdf _sourcePdf;
    private readonly IndirectObjectManager _indirectObjectManager;

    private readonly AsyncLazy<PageTreeNodeDictionary> _rootPageTreeNode;
    private AsyncLazy<List<IndirectObject>>? _pages;

    private readonly FormManager _formManager = new();

    /// <summary>
    /// Internal constructor for creating a <see cref="Pdf"/> instance from an <see cref="IPdf"/>.
    /// </summary>
    internal Pdf(IPdf sourcePdf)
    {
        _sourcePdf = sourcePdf ?? throw new ArgumentNullException(nameof(sourcePdf));
        _indirectObjectManager = new IndirectObjectManager(sourcePdf.IndirectObjects);

        _rootPageTreeNode = new AsyncLazy<PageTreeNodeDictionary>(async () =>
        {
            return await IndirectObjects.GetAsync<PageTreeNodeDictionary>(DocumentCatalog.Pages)
                ?? throw new InvalidPdfException("Unable to find root page tree node");
        });

        ResetPages();
    }

    private void ResetPages()
    {
        _pages = new AsyncLazy<List<IndirectObject>>(async () =>
        {
            var rootPageTreeNode = await _rootPageTreeNode;

            return await rootPageTreeNode.GetSubPagesAsync(IndirectObjects);
        });
    }

    #region IPdf

    public IIndirectObjectDictionary IndirectObjects => _indirectObjectManager;
    public Trailer? Trailer => _sourcePdf.Trailer;
    public IndirectObject? CrossReferenceStream => _sourcePdf.CrossReferenceStream;
    public DocumentCatalogDictionary DocumentCatalog => _sourcePdf.DocumentCatalog;

    public ITrailerDictionary TrailerDictionary => _sourcePdf.TrailerDictionary;

    // TODO: logic is duplicated in readonlypdf. Consider sharing.
    public async Task<Page> GetPageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));

        var pageIndirectObject = (await _pages!)[pageNumber - 1];

        return pageIndirectObject == null
            ? throw new InvalidOperationException()
            : new Page(pageIndirectObject, _indirectObjectManager);
    }

    public async Task<int> GetPageCountAsync() => (await _rootPageTreeNode).PageCount;

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

    public async Task<Page> AppendPageAsync(PageDictionary.PageCreationOptions? pageCreationOptions = null)
    {
        pageCreationOptions ??= PageDictionary.PageCreationOptions.Default;

        var rootPageTreeNodeIndirectObject = await IndirectObjects.GetAsync(DocumentCatalog.Pages)
            ?? throw new InvalidPdfException("Unable to find root page tree node");

        var page = PageDictionary.CreateNew(DocumentCatalog.Pages, pageCreationOptions);

        var pageIndirectObject = _indirectObjectManager.Add(page);

        var rootPageTreeNode = rootPageTreeNodeIndirectObject!.Get<PageTreeNodeDictionary>();

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

        var parent = (parentIndirectObject.Children.First() as PageTreeNodeDictionary)!;

        // TODO: Find pages which are subpages of this, move them so they don't become orphans

        parent.RemoveChild(page.IndirectObject.Id.Reference);

        await DecrementPageCountAsync(parent);

        _indirectObjectManager.Delete(page.IndirectObject.Id);
        _indirectObjectManager.Update(new IndirectObject(parentIndirectObject.Id, parent));
    }

    public async Task<Page> InsertPageAsync(int pageNumber, PageDictionary.PageCreationOptions? pageCreationOptions = null)
    {
        // get page at number
        // get parent page tree node
        // add new page indirect object
        // add new page ref to kids property
        // increment page count
        // - this involves recursively updating multiple nodes in page tree

        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

        pageCreationOptions ??= PageDictionary.PageCreationOptions.Default;

        var count = await GetPageCountAsync();

        if (pageNumber > count)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} must be less than or equal to the total number of pages. To add a page to the end of the PDF, use {nameof(AppendPageAsync)}");
        }

        var pageAtNumber = await GetPageAsync(pageNumber);

        var parentPageTreeNodeIndirectObject = await _indirectObjectManager.GetAsync(pageAtNumber.Dictionary.Parent);
        var parentPageTreeNode = parentPageTreeNodeIndirectObject!.Get<PageTreeNodeDictionary>();

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
        foreach (var page in await _pages!)
        {
            page.Get<PageDictionary>().SetRotation(rotation);
            _indirectObjectManager.Update(page);
        }
    }
    
    // TODO: support for all field types?
    public async Task CompleteFormAsync(IDictionary<string, string> formValues)
    {
        if (DocumentCatalog.AcroForm is null)
        {
            throw new InvalidOperationException("PDF does not contain a form");
        }

        var acroFormIndirectObject = await IndirectObjects.GetAsync(DocumentCatalog.AcroForm)
            ?? throw new InvalidPdfException("Unable to resolve form reference");

        var acroForm = acroFormIndirectObject.Get<InteractiveFormDictionary>();

        // Ensure compliant PDF viewers use the provided appearance stream for each field
        // This setting applies to pre-PDF2.0 documents.
        acroForm.SetNeedAppearances(false);

        _indirectObjectManager.Update(acroFormIndirectObject);

        // TODO: can we reuse an existing font?
        var font = new Type1FontDictionary("Helvetica");
        var fontIndirectObject = _indirectObjectManager.Add(font);

        // TODO: choose short unique font name
        var fontResourceName = "F1";
        var fontMap = new Dictionary<Name, IPdfObject> { { fontResourceName, fontIndirectObject.Id.Reference } };

        var fields = await new FormManager().GetFieldsAsync(IndirectObjects, acroForm.Fields.Cast<IndirectObjectReference>());

        foreach (var kvp in formValues)
        {
            var fieldIndirectObject = fields[kvp.Key];

            var fieldDict = fieldIndirectObject.Get<FieldDictionary>();

            if ((fieldDict.FT ?? "") != "Tx")
            {
                continue;
            }

            fieldDict.SetValue(kvp.Value!);

            // TODO: do we need to account for fields which already have an appearance stream? or always replace?
            var fieldSizeRect = Rectangle.FromSize(fieldDict.Rect.Width, fieldDict.Rect.Height);

            var textObject = new TextObject(
                kvp.Value!,
                fieldSizeRect,
                new Coordinate(2, 5), // TODO: calculate this
                new TextObject.FontOptions(fontResourceName, 12, RGBColour.Black)
                );

            var resourceDict = new ResourceDictionary(font: fontMap);

            var apFormXObject = new FormXObject(
                fieldSizeRect,
                [textObject],
                resourceDict,
                filters: null,
                sourceDataIsCompressed: false
                );

            var apIndirectObject = _indirectObjectManager.Add(apFormXObject);

            fieldDict.SetAppearanceStream(AppearanceDictionary.Create(apIndirectObject.Id.Reference));

            _indirectObjectManager.Update(fieldIndirectObject);
        }
    }

    // TODO: duplicate logic in ReadOnlyPdf. See if we can share it.
    public async Task<IEnumerable<FormField>> GetFieldsAsync()
    {
        List<FormField> fields = [];

        if (DocumentCatalog.AcroForm is null)
        {
            return fields;
        }

        var acroForm = await IndirectObjects.GetAsync<InteractiveFormDictionary>(DocumentCatalog.AcroForm)
            ?? throw new InvalidPdfException("Unable to resolve form reference");

        var fieldDict = await _formManager.GetFieldsAsync(IndirectObjects, acroForm.Fields.Cast<IndirectObjectReference>());

        return fieldDict.Select(kvp =>
        {
            var field = kvp.Value.Get<FieldDictionary>();

            return new FormField(kvp.Key, field.TU, _formManager.GetFieldValue(field.V));
        });
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
    private async Task IncrementPageCountAsync(PageTreeNodeDictionary pageTreeNode)
    {
        if (pageTreeNode.Parent is null)
        {
            return;
        }

        var parentPageTreeNodeIndirectObject = await _indirectObjectManager.GetAsync(pageTreeNode.Parent);
        var parentPageTreeNode = parentPageTreeNodeIndirectObject!.Get<PageTreeNodeDictionary>();

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
        var parentPageTreeNode = parentPageTreeNodeIndirectObject!.Get<PageTreeNodeDictionary>();

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
