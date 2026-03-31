using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Elements.Forms;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Graphics;
using ZingPDF.Graphics.Images;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Encryption;
using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.FileStructure;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text;
using ZingPDF.Text.Encoding.PDFDocEncoding;
using ZingPDF.Text.SimpleFonts;

namespace ZingPDF;

/// <summary>
/// Default implementation of <see cref="IPdf"/>.
/// </summary>
public class Pdf : IPdf, IDisposable
{
    private static readonly ServiceProvider _rootServices = new ServiceCollection()
        .AddDocumentServices()
        .AddParsers()
        .AddTextExtractor()
        .BuildServiceProvider();

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
    private PdfMetadata? _metadata;
    private bool _rewriteAllObjects;
    private bool _removeEncryptionOnSave;
    private bool _removeHistoryOnSave;
    private PdfEncryptionOptions? _pendingEncryptionOptions;

    private Pdf(Stream data)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));

        Data = data;

        _documentLifetime = _rootServices.CreateScope();
        _services = _documentLifetime.ServiceProvider;
        _services.GetRequiredService<PdfContextAccessor>().Pdf = this;

        Objects = _services.GetRequiredService<IPdfObjectCollection>();
        _encryptionProvider = _services.GetRequiredService<IPdfEncryptionProvider>();
    }

    /// <inheritdoc />
    public Stream Data { get; }

    /// <inheritdoc />
    public IPdfObjectCollection Objects { get; }

    /// <inheritdoc />
    public async Task AuthenticateAsync(string password)
    {
        await _encryptionProvider.AuthenticateAsync(password);
    }

    /// <inheritdoc />
    public Task<IList<IndirectObject>> GetAllPagesAsync() => Objects.PageTree.GetPagesAsync();

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<PdfMetadata> GetMetadataAsync()
    {
        _metadata ??= await PdfMetadata.LoadAsync(this);
        return _metadata;
    }

    /// <inheritdoc />
    public async Task<Page> GetPageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));

        var pageIndirectObject = (await Objects.PageTree.GetPagesAsync())[pageNumber - 1];

        return pageIndirectObject == null
            ? throw new InvalidOperationException()
            : new Page(pageIndirectObject, this);
    }

    /// <inheritdoc />
    public Task<int> GetPageCountAsync() => Objects.PageTree.GetPageCountAsync();

    /// <inheritdoc />
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

    /// <inheritdoc />
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
        if (pageCreationOptions.MediaBox is null)
        {
            pageCreationOptions.MediaBox = await pageAtNumber.Dictionary.MediaBox.GetAsync()
                ?? throw new Exception("This PDF does not have a default page size, you must therefore provide a PageCreationOptions.MediaBox property or ensure an ancestor has a value for this property."); // TODO: proper exception
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public Task<IEnumerable<ExtractedText>> ExtractTextAsync()
    {
        var textExtractor = _services.GetRequiredService<ITextExtractor>();

        return textExtractor.ExtractTextAsync();
    }

    /// <inheritdoc />
    public async Task AddWatermarkAsync(string text)
    {
        await AddWatermarkInternalAsync(text);
    }

    /// <inheritdoc />
    public async Task<PdfFont> RegisterStandardFontAsync(string fontName, string? resourceName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontName);

        var metricsProvider = new PDFStandardFontMetricsProvider();
        var metrics = metricsProvider.GetFontMetrics(fontName);
        var resolvedFontName = metrics.Name;

        if (resolvedFontName is StandardPdfFonts.Symbol or StandardPdfFonts.ZapfDingbats)
        {
            throw new NotSupportedException("High-level font registration currently supports WinAnsi text fonts only.");
        }

        var fontDictionary = new Type1FontDictionary(this, ObjectContext.UserCreated);
        fontDictionary.Set(Constants.DictionaryKeys.Font.BaseFont, (Name)resolvedFontName);
        fontDictionary.Set(Constants.DictionaryKeys.Font.Encoding, (Name)Text.Encoding.PDFEncoding.WinAnsi);

        var fontObject = await Objects.AddAsync(fontDictionary);

        return new PdfFont(
            (Name)(resourceName ?? UniqueStringGenerator.Generate()),
            fontObject.Reference,
            resolvedFontName,
            FontTextEncoding.WinAnsi,
            isEmbedded: false);
    }

    /// <inheritdoc />
    public async Task<PdfFont> RegisterTrueTypeFontAsync(string fontPath, string? resourceName = null, string? fontName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontPath);

        await using var stream = new FileStream(fontPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await RegisterTrueTypeFontAsync(stream, resourceName, fontName);
    }

    /// <inheritdoc />
    public async Task<PdfFont> RegisterTrueTypeFontAsync(Stream fontData, string? resourceName = null, string? fontName = null)
    {
        ArgumentNullException.ThrowIfNull(fontData);

        var fontFace = await TrueTypeFontLoader.LoadAsync(fontData, fontName);
        var embeddedFont = await CreateTrueTypeFontAsync(fontFace);

        return new PdfFont(
            (Name)(resourceName ?? UniqueStringGenerator.Generate()),
            embeddedFont.Reference,
            fontFace.FontName,
            FontTextEncoding.WinAnsi,
            isEmbedded: true);
    }

    /// <inheritdoc />
    public void Compress(int dpi, int quality)
    {
        CompressAsync(dpi, quality).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public Task RemoveHistoryAsync()
    {
        _rewriteAllObjects = true;
        _removeHistoryOnSave = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task EncryptAsync(
        string userPassword,
        string? ownerPassword = null,
        PdfEncryptionAlgorithm algorithm = PdfEncryptionAlgorithm.Rc4_128)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userPassword);

        var resolvedOwnerPassword = string.IsNullOrWhiteSpace(ownerPassword) ? userPassword : ownerPassword;

        _rewriteAllObjects = true;
        _removeEncryptionOnSave = false;
        _pendingEncryptionOptions = PdfEncryptionOptions.Create(userPassword, resolvedOwnerPassword, algorithm);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DecryptAsync(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        await AuthenticateAsync(password);
        _rewriteAllObjects = true;
        _removeEncryptionOnSave = true;
        _pendingEncryptionOptions = null;
    }

    /// <inheritdoc />
    public async Task AppendPdfAsync(Stream stream)
    {
        await new PdfMerger(this, Load(stream)).AppendAsync();
    }

    /// <inheritdoc />
    public async Task SaveAsync(Stream outputStream)
    {
        ArgumentNullException.ThrowIfNull(outputStream);
        if (!outputStream.CanWrite) throw new ArgumentException("Provided output stream must be writable", nameof(outputStream));
        if (!outputStream.CanSeek) throw new ArgumentException("Provided output stream must be seekable", nameof(outputStream));
        if (!_removeHistoryOnSave && !ReferenceEquals(outputStream, Data) && outputStream.Length != 0)
        {
            throw new ArgumentException("Provided output stream must be empty unless saving back to the source stream.", nameof(outputStream));
        }

        if (_form != null)
        {
            await _form.UpdateAsync();
        }

        var metadata = _metadata ?? await GetMetadataAsync();
        await metadata.UpdateAsync();

        var encryptionWritePlan = await _encryptionProvider.CreateWritePlanAsync(_pendingEncryptionOptions);
        if (_removeHistoryOnSave)
        {
            await SaveWithoutHistoryAsync(outputStream, metadata, encryptionWritePlan);
            await outputStream.FlushAsync();
            Dispose();
            return;
        }

        // Copy original PDF to output if required.
        if (outputStream.Length == 0)
        {
            Data.Position = 0;
            await Data.CopyToAsync(outputStream);
        }

        var incrementalUpdate = await Objects.GenerateUpdateDeltaAsync(_rewriteAllObjects);
        if (incrementalUpdate != null)
        {
            incrementalUpdate.EncryptionWritePlan = encryptionWritePlan;
            incrementalUpdate.InfoReferenceOverride = metadata.InfoReference;
            incrementalUpdate.RemoveEncryption = _removeEncryptionOnSave;

            await incrementalUpdate.WriteAsync(outputStream);
        }

        await outputStream.FlushAsync();

        Dispose();
    }

    /// <summary>
    /// Loads a PDF from a seekable input stream.
    /// </summary>
    public static Pdf Load(Stream pdfInputStream)
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));

        if (!pdfInputStream.CanSeek)
            throw new ArgumentException("Provided stream must be seekable");

        return new Pdf(pdfInputStream);
    }

    /// <summary>
    /// Creates a new blank PDF containing a single page.
    /// </summary>
    public static Pdf Create(Action<PageDictionary.PageCreationOptions>? configureOptions = null)
        => PdfBootstrapper.Create(configureOptions);

    // TODO: move to testable class?
    /// <summary>
    /// Recursively increment the page count of this page tree node and all its ancestors
    /// </summary>
    private async Task IncrementPageCountAsync(PageTreeNodeDictionary pageTreeNode, int delta = 1)
    {
        if (await pageTreeNode.Parent.GetRawValueAsync() is not IndirectObjectReference parentReference)
        {
            return;
        }

        var parentPageTreeNodeIndirectObject = await Objects.GetAsync(parentReference);
        var parentPageTreeNode = (PageTreeNodeDictionary)parentPageTreeNodeIndirectObject.Object;

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
        if (await pageTreeNode.Parent.GetRawValueAsync() is not IndirectObjectReference parentReference)
        {
            return;
        }

        var parentPageTreeNodeIndirectObject = await Objects.GetAsync(parentReference);
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

    private async Task<IndirectObject> CreateTrueTypeFontAsync(TrueTypeFontFace fontFace)
    {
        var fontProgramDictionary = new StreamDictionary(this, ObjectContext.UserCreated);
        fontProgramDictionary.Set<Number>(Constants.DictionaryKeys.Stream.Length, fontFace.FontData.Length);
        fontProgramDictionary.Set<Number>("Length1", fontFace.FontData.Length);

        var fontProgram = new StreamObject<IStreamDictionary>(
            new MemoryStream(fontFace.FontData, writable: false),
            fontProgramDictionary,
            ObjectContext.UserCreated);
        var fontProgramObject = await Objects.AddAsync(fontProgram);

        var descriptor = new FontDescriptorDictionary(this, ObjectContext.UserCreated);
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.FontName, (Name)fontFace.FontName);
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.Flags, (Number)CreateFontFlags(fontFace.Metrics));
        descriptor.Set(
            Constants.DictionaryKeys.FontDescriptor.FontBBox,
            Syntax.CommonDataStructures.Rectangle.FromCoordinates(
                new Elements.Drawing.Coordinate(fontFace.BoundingBox.Left, fontFace.BoundingBox.Bottom),
                new Elements.Drawing.Coordinate(fontFace.BoundingBox.Right, fontFace.BoundingBox.Top)));
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.ItalicAngle, (Number)fontFace.Metrics.ItalicAngle);
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.Ascent, (Number)fontFace.Metrics.Ascent);
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.Descent, (Number)(-Math.Abs(fontFace.Metrics.Descent)));
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.CapHeight, (Number)fontFace.Metrics.CapHeight);
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.XHeight, (Number)fontFace.Metrics.XHeight);
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.StemV, (Number)Math.Max(fontFace.Metrics.StandardVerticalWidth ?? 80, 1));
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.StemH, (Number)Math.Max(fontFace.Metrics.StandardHorizontalWidth ?? 80, 1));
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.AvgWidth, (Number)fontFace.AverageWidth);
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.MaxWidth, (Number)fontFace.MaxWidth);
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.MissingWidth, (Number)fontFace.MissingWidth);
        descriptor.Set(Constants.DictionaryKeys.FontDescriptor.FontFile2, fontProgramObject.Reference);

        var descriptorObject = await Objects.AddAsync(descriptor);

        var firstChar = 32;
        var lastChar = 255;
        var widths = new ArrayObject(
            [.. Enumerable.Range(32, 224).Select(code => (IPdfObject)(Number)fontFace.WidthsByCharacterCode[(byte)code])],
            ObjectContext.UserCreated);

        var fontDictionary = new TrueTypeFontDictionary(this, ObjectContext.UserCreated);
        fontDictionary.Set(Constants.DictionaryKeys.Font.BaseFont, (Name)fontFace.FontName);
        fontDictionary.Set(Constants.DictionaryKeys.Font.Encoding, (Name)Text.Encoding.PDFEncoding.WinAnsi);
        fontDictionary.Set(Constants.DictionaryKeys.Font.FirstChar, (Number)firstChar);
        fontDictionary.Set(Constants.DictionaryKeys.Font.LastChar, (Number)lastChar);
        fontDictionary.Set(Constants.DictionaryKeys.Font.Widths, widths);
        fontDictionary.Set(Constants.DictionaryKeys.Font.FontDescriptor, descriptorObject.Reference);

        return await Objects.AddAsync(fontDictionary);
    }

    private static int CreateFontFlags(FontMetrics metrics)
    {
        var flags = FontFlags.NonSymbolic;

        if (metrics.IsFixedPitch)
        {
            flags |= FontFlags.FixedPitch;
        }

        if (metrics.ItalicAngle != 0)
        {
            flags |= FontFlags.Italic;
        }

        return (int)flags;
    }

    private async Task SaveWithoutHistoryAsync(
        Stream outputStream,
        PdfMetadata metadata,
        EncryptionWritePlan? encryptionWritePlan)
    {
        if (ReferenceEquals(outputStream, Data))
        {
            using var rewrittenPdf = new MemoryStream();
            await WriteFreshPdfAsync(rewrittenPdf, metadata, encryptionWritePlan);
            rewrittenPdf.Position = 0;

            outputStream.Position = 0;
            outputStream.SetLength(0);
            await rewrittenPdf.CopyToAsync(outputStream);
            return;
        }

        outputStream.Position = 0;
        outputStream.SetLength(0);
        await WriteFreshPdfAsync(outputStream, metadata, encryptionWritePlan);
    }

    private async Task WriteFreshPdfAsync(
        Stream outputStream,
        PdfMetadata metadata,
        EncryptionWritePlan? encryptionWritePlan)
    {
        var pdfVersion = await GetPdfVersionAsync();
        await new Header(pdfVersion, ObjectContext.UserCreated).WriteAsync(outputStream);

        var latestTrailer = await Objects.GetLatestTrailerDictionaryAsync();
        var allObjects = new List<IndirectObject>();
        await foreach (var obj in Objects)
        {
            allObjects.Add(obj);
        }

        allObjects.Sort(static (left, right) => left.Id.Index.CompareTo(right.Id.Index));

        var writtenObjects = new List<IndirectObject>(allObjects.Count);
        foreach (var entry in allObjects)
        {
            IndirectObject objectToWrite = entry;
            var encryptionObjectId = encryptionWritePlan?.EncryptReference?.Id;
            if (encryptionWritePlan != null && (encryptionObjectId is null || encryptionObjectId != entry.Id))
            {
                objectToWrite = _removeEncryptionOnSave
                    ? await EncryptionObjectTransformer.DecryptAsync(entry, encryptionWritePlan.Handler)
                    : await EncryptionObjectTransformer.EncryptAsync(entry, encryptionWritePlan.Handler);
            }

            await objectToWrite.WriteAsync(outputStream);
            writtenObjects.Add(objectToWrite);
        }

        var xrefTable = new CrossReferenceTable(BuildFreshCrossReferenceSections(writtenObjects), ObjectContext.UserCreated);
        await xrefTable.WriteAsync(outputStream);

        var originalId = (IPdfObject?)encryptionWritePlan?.OriginalFileId
            ?? latestTrailer.ID?[0]
            ?? PdfString.FromBytes(Guid.NewGuid().ToByteArray(), PdfStringSyntax.Hex, ObjectContext.UserCreated);
        var updateId = PdfString.FromBytes(Guid.NewGuid().ToByteArray(), PdfStringSyntax.Hex, ObjectContext.UserCreated);
        var fileIdentifier = new ArrayObject([originalId, updateId], ObjectContext.UserCreated);
        var encryptReference = _removeEncryptionOnSave
            ? null
            : (IPdfObject?)encryptionWritePlan?.EncryptReference
                ?? latestTrailer.GetAs<IndirectObjectReference>(Constants.DictionaryKeys.Trailer.Encrypt);
        var rootReference = latestTrailer.Root
            ?? throw new InvalidPdfException("Unable to save PDF because the latest trailer is missing the Root entry.");

        var trailer = new Trailer(
            TrailerDictionary.CreateNew(
                GetFreshXrefSize(writtenObjects),
                null,
                rootReference,
                encryptReference,
                metadata.InfoReference ?? latestTrailer.Info,
                fileIdentifier,
                this,
                ObjectContext.UserCreated),
            xrefTable.ByteOffset!.Value,
            ObjectContext.UserCreated);

        await trailer.WriteAsync(outputStream);
    }

    private static List<CrossReferenceSection> BuildFreshCrossReferenceSections(IEnumerable<IndirectObject> writtenObjects)
    {
        var objectsByIndex = writtenObjects.ToDictionary(x => x.Id.Index);
        var maxIndex = objectsByIndex.Count == 0 ? 0 : objectsByIndex.Keys.Max();
        var section = new CrossReferenceSection(0, ObjectContext.UserCreated);
        section.Add(CrossReferenceEntry.RootFreeEntry);

        for (var index = 1; index <= maxIndex; index++)
        {
            if (objectsByIndex.TryGetValue(index, out var obj))
            {
                section.Add(new CrossReferenceEntry(
                    obj.ByteOffset!.Value,
                    obj.Id.GenerationNumber,
                    inUse: true,
                    compressed: false,
                    ObjectContext.UserCreated));
            }
            else
            {
                section.Add(new CrossReferenceEntry(
                    0,
                    0,
                    inUse: false,
                    compressed: false,
                    ObjectContext.UserCreated));
            }
        }

        return [section];
    }

    private static int GetFreshXrefSize(IEnumerable<IndirectObject> writtenObjects)
        => writtenObjects.Any() ? writtenObjects.Max(x => x.Id.Index) + 1 : 1;

    private async Task<double> GetPdfVersionAsync()
    {
        var originalPosition = Data.Position;

        try
        {
            Data.Position = 0;
            byte[] headerBytes = new byte[8];
            var read = await Data.ReadAsync(headerBytes, 0, headerBytes.Length);
            if (read < headerBytes.Length)
            {
                throw new InvalidPdfException("Unable to read the PDF header.");
            }

            var version = Encoding.ASCII.GetString(headerBytes, 5, 3);
            return double.Parse(version, System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            Data.Position = originalPosition;
        }
    }

    private async Task CompressAsync(int dpi, int quality)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(dpi, 1, nameof(dpi));
        ArgumentOutOfRangeException.ThrowIfLessThan(quality, 1, nameof(quality));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quality, 100, nameof(quality));

        await foreach (var obj in Objects)
        {
            if (obj.Object is StreamObject<ImageDictionary> imageStream
                && await TryRecompressJpegImageAsync(imageStream, quality) is StreamObject<ImageDictionary> recompressedImage)
            {
                Objects.Update(new IndirectObject(obj.Id, recompressedImage));
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

            // Apply each rewritten stream immediately so compression does not need
            // to retain all transformed stream payloads in memory until the end.
            Objects.Update(new IndirectObject(obj.Id, new StreamObject<IStreamDictionary>(compressedData, newStreamDictionary)));
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

    /// <summary>
    /// Disposes the document stream and scoped services used by this PDF instance.
    /// </summary>
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
