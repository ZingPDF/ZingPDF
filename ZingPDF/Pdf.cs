using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Elements.Forms;
using ZingPDF.Extensions;
using ZingPDF.Graphics;
using ZingPDF.Graphics.Images;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Encryption;
using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text.Encoding.PDFDocEncoding;
using ZingPDF.Text.SimpleFonts;

namespace ZingPDF;

public class Pdf : IPdf, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly IServiceScope _documentLifetime;
    private readonly IPdfEncryptionProvider _encryptionProvider;

    internal const string _pdfContextKey = "PdfContext";

    static Pdf()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding.RegisterProvider(PDFDocEncodingProvider.Instance);
    }

    private Form? _form;
    private bool _rewriteAllObjects;
    private bool _removeEncryptionOnSave;
    private bool _encryptOnSaveRequested;

    private Pdf(Stream data)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));

        Data = data;

        _services = new ServiceCollection()
            .AddContext(this)
            .AddParsers()
            .AddTextExtractor()
            .BuildServiceProvider();

        _documentLifetime = _services.CreateScope();

        Objects = _services.GetRequiredService<IPdfObjectCollection>();
        _encryptionProvider = _services.GetRequiredService<IPdfEncryptionProvider>();
    }

    public Stream Data { get; }
    public IPdfObjectCollection Objects { get; }

    public async Task AuthenticateAsync(string password)
    {
        await _encryptionProvider.AuthenticateAsync(password);
    }

    public Task<IList<IndirectObject>> GetAllPagesAsync() => Objects.PageTree.GetPagesAsync();

    public async Task<Form?> GetFormAsync()
    {
        var documentCatalog = await Objects.GetDocumentCatalogAsync();

        if (documentCatalog.AcroForm is null)
        {
            return null;
        }

        var contentStreamParser = _services.GetRequiredService<IParser<ContentStream>>();

        _form = new Form(documentCatalog.AcroForm, this, contentStreamParser);

        return _form;
    }

    public async Task<Page> GetPageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));

        var pageIndirectObject = (await Objects.PageTree.GetPagesAsync())[pageNumber - 1];

        return pageIndirectObject == null
            ? throw new InvalidOperationException()
            : new Page(pageIndirectObject, this);
    }

    public Task<int> GetPageCountAsync() => Objects.PageTree.GetPageCountAsync();

    public async Task<Page> AppendPageAsync(Action<PageDictionary.PageCreationOptions>? configureOptions = null)
    {
        var pageCreationOptions = PageDictionary.PageCreationOptions.Initialize(configureOptions);

        var rootPageTreeNodeIndirectObject = await Objects.PageTree.GetRootPageTreeNodeAsync();

        DocumentCatalogDictionary documentCatalog = await Objects.GetDocumentCatalogAsync();

        var page = PageDictionary.CreateNew((await documentCatalog.Pages.GetRawValueAsync() as IndirectObjectReference)!, this, pageCreationOptions);

        var pageIndirectObject = await Objects.AddAsync(page);

        var rootPageTreeNode = (PageTreeNodeDictionary)rootPageTreeNodeIndirectObject.Object;

        // TODO: For now, to simplify adding pages,
        // new pages are appended to the root page tree node.
        // Determine if there's a better way, like ensuring a balanced tree.
        await rootPageTreeNode.AddChildAsync(pageIndirectObject.Reference);

        Objects.Update(rootPageTreeNodeIndirectObject);
        Objects.PageTree.Reset();

        return new Page(pageIndirectObject, this);
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
            this,
            pageCreationOptions
            );

        var newPageIndirectObject = await Objects.AddAsync(page);

        await parentPageTreeNode.InsertChildAsync(kidsIndex, newPageIndirectObject.Reference);

        await IncrementPageCountAsync(parentPageTreeNode);

        Objects.Update(parentPageTreeNodeIndirectObject);
        Objects.PageTree.Reset();

        return new Page(newPageIndirectObject, this);
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

        Objects.Delete(page.IndirectObject.Id);
        Objects.Update(new IndirectObject(parentIndirectObject.Id, parent));
        Objects.PageTree.Reset();
    }

    public async Task SetRotationAsync(Rotation rotation)
    {
        ArgumentNullException.ThrowIfNull(rotation);

        // Each page may have a rotation property already, therefore a loop is required to set all.
        // i.e. you can't just set an inheritable property on the root page tree node.
        foreach (var page in await Objects.PageTree.GetPagesAsync())
        {
            ((PageDictionary)page.Object).SetRotation(rotation);
            Objects.Update(page);
        }
    }

    public Task<IEnumerable<ExtractedText>> ExtractTextAsync()
    {
        var textExtractor = _services.GetRequiredService<ITextExtractor>();

        return textExtractor.ExtractTextAsync();
    }

    public async Task AddWatermarkAsync(string text)
    {
        await AddWatermarkInternalAsync(text);
    }

    public void Compress(int dpi, int quality)
    {
        CompressAsync(dpi, quality).GetAwaiter().GetResult();
    }

    public async Task DecompressAsync()
    {
        List<IndirectObject> toBeUpdated = [];

        await foreach(var obj in Objects)
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
            Objects.Update(io);
        }
    }

    public void Encrypt()
    {
        _encryptOnSaveRequested = true;
        _removeEncryptionOnSave = false;
    }

    public void Decrypt()
    {
        _rewriteAllObjects = true;
        _removeEncryptionOnSave = true;
    }

    public async Task AppendPdfAsync(Stream stream)
    {
        await new PdfMerger(this, Load(stream)).AppendAsync();
    }

    public async Task SaveAsync(Stream outputStream)
    {
        ArgumentNullException.ThrowIfNull(outputStream);
        if (!outputStream.CanWrite) throw new ArgumentException("Provided output stream must be writable", nameof(outputStream));

        // Copy original PDf to output if required.
        if (outputStream.Length == 0)
        {
            Data.Position = 0;
            await Data.CopyToAsync(outputStream);
        }

        if (_form != null)
        {
            await _form.UpdateAsync();
        }

        var incrementalUpdate = await Objects.GenerateUpdateDeltaAsync(_rewriteAllObjects);
        if (incrementalUpdate != null)
        {
            incrementalUpdate.EncryptionWritePlan = await _encryptionProvider.CreateWritePlanAsync();
            incrementalUpdate.RemoveEncryption = _removeEncryptionOnSave;

            if (_encryptOnSaveRequested && incrementalUpdate.EncryptionWritePlan is null)
            {
                throw new NotSupportedException("Encrypting a previously unencrypted PDF is not yet supported by this API.");
            }

            await incrementalUpdate.WriteAsync(outputStream);
        }

        await outputStream.FlushAsync();

        Dispose();
    }

    public static Pdf Load(Stream pdfInputStream)
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));

        if (!pdfInputStream.CanSeek)
            throw new ArgumentException("Provided stream must be seekable");

        return new Pdf(pdfInputStream);
    }

    public static Pdf Create(Action<PageDictionary.PageCreationOptions>? configureOptions = null)
        => PdfBootstrapper.Create(configureOptions);

    // TODO: move to testable class?
    /// <summary>
    /// Recursively increment the page count of this page tree node and all its ancestors
    /// </summary>
    private async Task IncrementPageCountAsync(PageTreeNodeDictionary pageTreeNode, int delta = 1)
    {
        PageTreeNodeDictionary? parentPageTreeNode = await pageTreeNode.Parent.GetAsync();
        if (parentPageTreeNode is null)
        {
            return;
        }

        var parentPageTreeNodeIndirectObject = await pageTreeNode.Parent.GetIndirectObjectAsync();

        await parentPageTreeNode.IncrementCountAsync(delta);

        Objects.Update(parentPageTreeNodeIndirectObject);

        await IncrementPageCountAsync(parentPageTreeNode, delta);
    }

    // TODO: move to testable class?
    /// <summary>
    /// Recursively decrement the page count of this page tree node and all its ancestors
    /// </summary>
    private async Task DecrementPageCountAsync(PageTreeNodeDictionary pageTreeNode, int delta = 1)
    {
        if (pageTreeNode.Parent is null)
        {
            return;
        }

        var parentPageTreeNodeIndirectObject = await pageTreeNode.Parent.GetIndirectObjectAsync();
        var parentPageTreeNode = (PageTreeNodeDictionary)parentPageTreeNodeIndirectObject.Object;

        await parentPageTreeNode.DecrementCountAsync(delta);

        Objects.Update(parentPageTreeNodeIndirectObject);

        await DecrementPageCountAsync(parentPageTreeNode, delta);
    }

    private async Task AddWatermarkInternalAsync(string text)
    {
        var watermarkFont = new Type1FontDictionary(this, ObjectContext.UserCreated);
        watermarkFont.Set(Constants.DictionaryKeys.Font.BaseFont, (Name)"Helvetica");
        watermarkFont.Set(Constants.DictionaryKeys.Font.Encoding, (Name)Text.Encoding.PDFEncoding.WinAnsi);

        var fontObject = await Objects.AddAsync(watermarkFont);
        var fontResourceName = (Name)UniqueStringGenerator.Generate();

        foreach (var pageObject in await Objects.PageTree.GetPagesAsync())
        {
            var page = new Page(pageObject, this);
            var mediaBox = await page.Dictionary.MediaBox.GetAsync();
            var resources = ResourceDictionary.FromDictionary(await page.Dictionary.Resources.GetAsync());
            await resources.AddFontAsync(fontResourceName, fontObject.Reference, this);
            page.Dictionary.SetResources(resources);

            var pageWidth = mediaBox.UpperRight.X - mediaBox.LowerLeft.X;
            var pageHeight = mediaBox.UpperRight.Y - mediaBox.LowerLeft.Y;
            var fontSize = 42;
            var x = mediaBox.LowerLeft.X + (pageWidth * 0.2);
            var y = mediaBox.LowerLeft.Y + (pageHeight * 0.5);

            var watermarkContent = new ContentStream()
                .SaveGraphicsState()
                .SetColour(new RGBColour(0.8, 0.8, 0.8))
                .BeginTextObject()
                .SetTextState(fontResourceName, fontSize)
                .SetTextMatrix(
                    1,
                    0,
                    0,
                    1,
                    x,
                    y)
                .ShowText(PdfString.FromTextAuto(text, ObjectContext.UserCreated))
                .EndTextObject()
                .RestoreGraphicsState();

            var watermarkStream = await new ContentStreamFactory([watermarkContent])
                .CreateAsync(new StreamDictionary(this, ObjectContext.UserCreated), ObjectContext.UserCreated);

            var watermarkIndirectObject = await Objects.AddAsync(watermarkStream);

            await page.Dictionary.AddContentAsync(watermarkIndirectObject.Reference);

            Objects.Update(pageObject);
        }
    }

    private async Task CompressAsync(int dpi, int quality)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(dpi, 1, nameof(dpi));
        ArgumentOutOfRangeException.ThrowIfLessThan(quality, 1, nameof(quality));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quality, 100, nameof(quality));

        List<IndirectObject> toBeUpdated = [];

        await foreach (var obj in Objects)
        {
            if (obj.Object is StreamObject<ImageDictionary> imageStream
                && await TryRecompressJpegImageAsync(imageStream, quality) is StreamObject<ImageDictionary> recompressedImage)
            {
                toBeUpdated.Add(new IndirectObject(obj.Id, recompressedImage));
                continue;
            }

            if (obj.Object is not IStreamObject streamObj)
            {
                continue;
            }

            ArrayObject? filterNames = await streamObj.Dictionary.Filter.GetAsync();
            if (filterNames is not null && filterNames.Any())
            {
                continue;
            }

            var rawData = await streamObj.GetDecompressedDataAsync();
            var compressedData = new FlateDecodeFilter(null).Encode(rawData);
            rawData.Position = 0;

            var newStreamDictionary = StreamDictionary.FromDictionary(streamObj.Dictionary);
            newStreamDictionary.Set(Constants.DictionaryKeys.Stream.Filter, new ShorthandArrayObject([(Name)Constants.Filters.Flate], ObjectContext.UserCreated));
            newStreamDictionary.Set(Constants.DictionaryKeys.Stream.Length, (ZingPDF.Syntax.Objects.Number)compressedData.Length);
            newStreamDictionary.Set(Constants.DictionaryKeys.Stream.DL, (ZingPDF.Syntax.Objects.Number)rawData.Length);

            toBeUpdated.Add(new IndirectObject(obj.Id, new StreamObject<IStreamDictionary>(compressedData, newStreamDictionary)));
        }

        foreach (var indirectObject in toBeUpdated)
        {
            Objects.Update(indirectObject);
        }
    }

    private static async Task<StreamObject<ImageDictionary>?> TryRecompressJpegImageAsync(StreamObject<ImageDictionary> imageStream, int quality)
    {
        var filters = await imageStream.Dictionary.Filter.GetAsync();
        if (filters is null || !filters.Cast<Name>().Any(x => x.Value == Constants.Filters.DCT))
        {
            return null;
        }

        try
        {
            imageStream.Data.Position = 0;
            using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(imageStream.Data);
            var output = new MemoryStream();
            await image.SaveAsync(output, new JpegEncoder { Quality = quality });
            output.Position = 0;

            var dictionary = (ImageDictionary)imageStream.Dictionary.Clone();
            dictionary.Set(Constants.DictionaryKeys.Stream.Length, (ZingPDF.Syntax.Objects.Number)output.Length);
            dictionary.Set(Constants.DictionaryKeys.Stream.DL, (ZingPDF.Syntax.Objects.Number)output.Length);

            return new StreamObject<ImageDictionary>(output, dictionary);
        }
        catch
        {
            return null;
        }
        finally
        {
            imageStream.Data.Position = 0;
        }
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
            ((IDisposable)Data).Dispose();

            _documentLifetime.Dispose();
    }

}

    #endregion
}
