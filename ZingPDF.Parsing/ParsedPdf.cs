using ZingPDF.Objects.Pages;
using ZingPDF.Objects.Primitives;
using ZingPDF.Objects.Primitives.IndirectObjects;
using ZingPDF.Drawing;
using ZingPDF.Objects;
using ZingPDF.Objects.DataStructures;
using ZingPDF.Parsing.IncrementalUpdates;

namespace ZingPDF.Parsing;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// This class is disposable. Disposing will dispose the underlying <see cref="Stream"/>.
/// </remarks>
public class Pdf : IDisposable
{
    private readonly Stream _pdfInputStream;
    private readonly EditablePdfNavigator _pdfNavigator;

    private readonly CrossReferenceGenerator _crossReferenceGenerator = new();

    /// <summary>
    /// Private constructor for creating a PDF from a content stream.
    /// </summary>
    private Pdf(Stream contentStream)
    {
        _pdfInputStream = contentStream ?? throw new ArgumentNullException(nameof(contentStream));

        _pdfNavigator = new EditablePdfNavigator(new PdfFileNavigator(contentStream));
    }

    /// <summary>
    /// Load a PDF from a stream.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> which contains the PDF data.</param>
    /// <returns>A <see cref="Pdf"/> instance.</returns>
    /// <example>
    /// <![CDATA[
    /// using var inputFileStream = new FileStream("example.pdf", FileMode.Open);
    /// 
    /// var pdf = Pdf.Load(inputFileStream);
    /// ]]>
    /// </example>
    /// <remarks>
    /// This method does not parse or validate the PDF, it simply provides a <see cref="Pdf"/> 
    /// instance linked to the provided <see cref="Stream"/>.
    /// Further operations will access the stream efficiently as required.<para></para>
    /// </remarks>
    /// <exception cref="ArgumentException"></exception>
    public static Pdf Load(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable", nameof(stream));
        }

        return new(stream);
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
        if (outputStream.Length == 0)
        {
            _pdfInputStream.Position = 0;
            await _pdfInputStream.CopyToAsync(outputStream);
        }

        var latestUpdate = _pdfNavigator.GetWorkingIncrementalUpdate();

        if (latestUpdate.NewOrUpdatedObjects.Count != 0 || latestUpdate.DeletedObjects.Count != 0)
        {
            await latestUpdate.WriteAsync(outputStream);
        }

        await outputStream.FlushAsync();
    }

    public async Task<int> GetPageCountAsync()
    {
        var rootPageTreeNodeIndirectObject = await _pdfNavigator.GetRootPageTreeNodeAsync();

        var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

        return rootPageTreeNode.PageCount;
    }

    /// <summary>
    /// Get the <see cref="Page"/> at the specified number.<para></para>
    /// </summary>
    /// <param name="pageNumber">The page number to return. Pages start at number 1 for the first page.</param>
    /// <returns>a <see cref="Page"/> instance representing the page at the specified number.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task<Page> GetPageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

        // TODO: check if there's a more efficient way to do this.
        var pages = await _pdfNavigator.GetPagesAsync();

        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageNumber, pages.Count());

        return (pages.ElementAt(pageNumber - 1).Children.First() as Page)!;
    }

    // TODO: This just appends a blank page. Do we need to accept a Page object here?
    // If so, how would a user consume it, Page creation requires knowledge of the parent.
    // Or maybe we accept page creation options.
    public async Task AppendPageAsync()
    {
        var rootPageTreeNodeIndirectObject = await _pdfNavigator.GetRootPageTreeNodeAsync();

        var page = Page.CreateNew(rootPageTreeNodeIndirectObject.Id.Reference, new Page.PageCreationOptions { MediaBox = new Rectangle(new(0, 0), new(200, 200)) });

        var pageIndirectObject = await _pdfNavigator.AddNewObjectAsync(page);

        var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

        // TODO: For now, to simplify adding pages,
        // new pages are appended to the root page tree node.
        // Determine if there's a better way, like ensuring a balanced tree.
        rootPageTreeNode.Kids.Add(pageIndirectObject.Id.Reference);

        rootPageTreeNode.PageCount++;

        _pdfNavigator.UpdateObject(rootPageTreeNodeIndirectObject);
    }

    public async Task InsertPageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

        var count = await GetPageCountAsync();

        if (pageNumber > count)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} must be less than or equal to the total number of pages. To add a page to the end of the PDF, use {nameof(AppendPageAsync)}");
        }

        // TODO: check if there's a more efficient way to do this.
        var pages = await _pdfNavigator.GetPagesAsync();

        // Find the page, find its parent, insert new page into kids property
        var pageAtNumberIndirectObject = pages.ElementAt(pageNumber - 1);
        var pageAtNumber = (pageAtNumberIndirectObject.Children.First() as Page)!;
        var parentPageTreeNodeIndirectObject = await _pdfNavigator.DereferenceIndirectObjectAsync(pageAtNumber.Parent);
        var parentPageTreeNode = (parentPageTreeNodeIndirectObject.Children.First() as PageTreeNode)!;

        var kidsIndex = parentPageTreeNode.Kids.ToList().IndexOf(pageAtNumberIndirectObject.Id.Reference);

        var page = Page.CreateNew(
            parentPageTreeNodeIndirectObject.Id.Reference,
            new Page.PageCreationOptions { MediaBox = new Rectangle(new(0, 0), new(200, 200)) }
            );

        var newPageIndirectObject = await _pdfNavigator.AddNewObjectAsync(page);

        var newKids = parentPageTreeNode.Kids.ToList();
        newKids.Insert(kidsIndex, newPageIndirectObject.Id.Reference);

        parentPageTreeNode.Kids = newKids.ToArray();
        parentPageTreeNode.PageCount++;

        _pdfNavigator.UpdateObject(parentPageTreeNodeIndirectObject);
    }

    public void ReplacePage(int pageNumber, Page page)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentNullException.ThrowIfNull(page);

        throw new NotImplementedException();
    }

    public async Task DeletePageAsync(int pageNumber)
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

    public async Task SetPageRotationAsync(int pageNumber, Rotation rotation)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentNullException.ThrowIfNull(rotation);

        // TODO: check if there's a more efficient way to do this.
        var pages = await _pdfNavigator.GetPagesAsync();

        var page = pages.ElementAt(pageNumber - 1);

        (page.Children.First() as Page)!.Rotate = rotation;

        _pdfNavigator.UpdateObject(page);
    }

    public void Draw(
        int pageNumber,
        IEnumerable<Drawing.Path> paths,
        IEnumerable<Text> text,
        IEnumerable<Image> imageOperations,
        CoordinateSystem coordinateSystem = CoordinateSystem.BottomUp
        )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

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

    public void Compress(int dpi = 144, int quality = 75)
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
}
