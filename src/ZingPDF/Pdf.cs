using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Elements.Forms;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Graphics;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.Graphics.Images;
using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Signing;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
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
using static ZingPDF.Syntax.ContentStreamsAndResources.ContentStream.Operators;

namespace ZingPDF;

/// <summary>
/// Default implementation of <see cref="IPdf"/>.
/// </summary>
public class Pdf : IPdf, IDisposable
{
    private const int PageTreeBranchFactor = 32;

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
    private IndirectObjectReference? _appendLeafHint;
    private bool _rewriteAllObjects;
    private bool _removeEncryptionOnSave;
    private bool _removeHistoryOnSave;
    private PdfEncryptionOptions? _pendingEncryptionOptions;
    private PendingPdfSignature? _pendingSignature;

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

        if (await documentCatalog.AcroForm.GetAsync() is null)
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

        var pageIndirectObject = await Objects.PageTree.GetPageAsync(pageNumber);

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
        var appendParentIndirectObject = await EnsureAppendLeafAsync();
        var page = PageDictionary.CreateNew(appendParentIndirectObject.Reference, this, pageCreationOptions);
        var pageIndirectObject = await Objects.AddAsync(page);
        var appendParent = (PageTreeNodeDictionary)appendParentIndirectObject.Object;

        await appendParent.AddChildAsync(pageIndirectObject.Reference);
        Objects.Update(appendParentIndirectObject);
        _appendLeafHint = appendParentIndirectObject.Reference;

        if (await appendParent.Parent.GetRawValueAsync() is IndirectObjectReference)
        {
            await IncrementPageCountAsync(appendParent);
        }

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

        var (pageAtNumberIndirectObject, parentPageTreeNodeIndirectObject, kidsIndex) = await Objects.PageTree.GetPageLocationAsync(pageNumber);
        var pageAtNumber = new Page(pageAtNumberIndirectObject, this);
        var parentPageTreeNode = (PageTreeNodeDictionary)parentPageTreeNodeIndirectObject.Object;

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
        _appendLeafHint = null;
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

        var (pageIndirectObject, parentIndirectObject, _) = await Objects.PageTree.GetPageLocationAsync(pageNumber);
        var page = new Page(pageIndirectObject, this);
        var parent = (PageTreeNodeDictionary)parentIndirectObject.Object;

        // TODO: Find pages which are subpages of this, move them so they don't become orphans

        await parent.RemoveChildAsync(page.IndirectObject.Reference);

        await DecrementPageCountAsync(parent);
        await PruneEmptyPageTreeNodesAsync(parentIndirectObject);

        Objects.Delete(page.IndirectObject.Id);
        _appendLeafHint = null;
        Objects.PageTree.Reset();
    }

    /// <inheritdoc />
    public async Task<Pdf> ExportPagesAsync(IEnumerable<int> pageNumbers)
    {
        var selectedPageNumbers = await NormalizeSelectedPageNumbersAsync(pageNumbers);
        var exportedPdf = PdfBootstrapper.CreateEmpty();

        var copier = new PdfObjectGraphCopier(exportedPdf, this);

        foreach (var pageNumber in selectedPageNumbers)
        {
            var sourcePage = await GetPageAsync(pageNumber);
            await exportedPdf.AppendCopiedPageAsync(sourcePage.IndirectObject, copier);
        }

        return exportedPdf;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Pdf>> SplitAsync(int pagesPerDocument)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pagesPerDocument, 1, nameof(pagesPerDocument));

        var pageCount = await GetPageCountAsync();
        var documents = new List<Pdf>((pageCount + pagesPerDocument - 1) / pagesPerDocument);

        for (var startPage = 1; startPage <= pageCount; startPage += pagesPerDocument)
        {
            var count = Math.Min(pagesPerDocument, pageCount - startPage + 1);
            documents.Add(await ExportPagesAsync(Enumerable.Range(startPage, count)));
        }

        return documents;
    }

    /// <inheritdoc />
    public async Task SetRotationAsync(Rotation rotation)
    {
        ArgumentNullException.ThrowIfNull(rotation);

        // Each page may have a rotation property already, therefore a loop is required to set all.
        // i.e. you can't just set an inheritable property on the root page tree node.
        await foreach (var page in Objects.PageTree.EnumeratePagesAsync())
        {
            ((PageDictionary)page.Object).SetRotation(rotation);
            Objects.Update(page);
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<ExtractedText>> ExtractTextAsync()
    {
        return _services.GetRequiredService<ITextExtractor>().ExtractTextAsync();
    }

    public Task<IEnumerable<ExtractedText>> ExtractTextAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));

        return _services.GetRequiredService<ITextExtractor>().ExtractTextAsync(pageNumber);
    }

    /// <inheritdoc />
    public Task<TextExtractionResult> ExtractTextAsync(TextExtractionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return _services.GetRequiredService<ITextExtractor>().ExtractTextAsync(options);
    }

    /// <inheritdoc />
    public Task<TextExtractionResult> ExtractTextAsync(int pageNumber, TextExtractionOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));
        ArgumentNullException.ThrowIfNull(options);

        return _services.GetRequiredService<ITextExtractor>().ExtractTextAsync(pageNumber, options);
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
        PdfEncryptionAlgorithm algorithm = PdfEncryptionAlgorithm.Rc4_128,
        PdfEncryptionPermissions permissions = PdfEncryptionPermissions.All)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userPassword);

        var resolvedOwnerPassword = string.IsNullOrWhiteSpace(ownerPassword) ? userPassword : ownerPassword;

        _rewriteAllObjects = true;
        _removeEncryptionOnSave = false;
        _pendingEncryptionOptions = PdfEncryptionOptions.Create(
            userPassword,
            resolvedOwnerPassword,
            algorithm,
            PdfEncryptionPermissionBits.ToStandardPermissionValue(permissions));

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
        _appendLeafHint = null;
    }

    /// <inheritdoc />
    public async Task SignInvisibleAsync(X509Certificate2 certificate, PdfSignatureOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var resolvedOptions = CreateInvisibleSignatureOptions(options);
        var signatureField = await EnsureHiddenSignatureFieldAsync(resolvedOptions.FieldName);

        await QueueSignatureAsync(signatureField, certificate, resolvedOptions);
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
        if (_pendingSignature is not null)
        {
            if (encryptionWritePlan is not null || _removeEncryptionOnSave || _pendingEncryptionOptions is not null)
            {
                throw new NotSupportedException("Signing encrypted output is not implemented yet.");
            }

            using var stagedOutput = new MemoryStream();
            await WriteDocumentAsync(stagedOutput, metadata, encryptionWritePlan);
            FinalizePendingSignature(stagedOutput, _pendingSignature);

            stagedOutput.Position = 0;
            outputStream.Position = 0;
            outputStream.SetLength(0);
            await stagedOutput.CopyToAsync(outputStream);
            await outputStream.FlushAsync();
            Dispose();
            return;
        }

        await WriteDocumentAsync(outputStream, metadata, encryptionWritePlan);
        await outputStream.FlushAsync();

        Dispose();
    }

    internal async Task QueueSignatureAsync(
        IndirectObject signatureFieldIndirectObject,
        X509Certificate2 certificate,
        PdfSignatureOptions options)
    {
        ArgumentNullException.ThrowIfNull(signatureFieldIndirectObject);
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentNullException.ThrowIfNull(options);

        if (!certificate.HasPrivateKey)
        {
            throw new ArgumentException("The signing certificate must include a private key.", nameof(certificate));
        }

        if (_pendingSignature is not null)
        {
            throw new NotSupportedException("Only one pending signature is supported per save operation.");
        }

        if (options.EstimatedSignatureSizeBytes < 2048)
        {
            throw new ArgumentOutOfRangeException(nameof(options.EstimatedSignatureSizeBytes), "EstimatedSignatureSizeBytes must be at least 2048.");
        }

        var latestTrailer = await Objects.GetLatestTrailerDictionaryAsync();
        if (await latestTrailer.Encrypt.GetAsync() is not null)
        {
            throw new NotSupportedException("Signing encrypted PDFs is not implemented yet.");
        }

        var fieldDictionary = (FieldDictionary)signatureFieldIndirectObject.Object;
        if (await fieldDictionary.V.GetAsync() is not null)
        {
            throw new InvalidOperationException("The signature field already contains a value.");
        }

        var signingDate = options.SigningDate ?? DateTimeOffset.UtcNow;
        var signatureDictionary = new Syntax.Objects.Dictionaries.Dictionary(new Dictionary<string, IPdfObject>
        {
            [Constants.DictionaryKeys.Type] = (Name)"Sig",
            ["Filter"] = (Name)"Adobe.PPKLite",
            [Constants.DictionaryKeys.Encryption.SubFilter] = (Name)"adbe.pkcs7.detached",
            ["ByteRange"] = new RawPdfSyntaxObject("[0000000000 0000000000 0000000000 0000000000]", ObjectContext.UserCreated),
            [Constants.DictionaryKeys.Annotation.Contents] = PdfString.FromBytes(new byte[options.EstimatedSignatureSizeBytes], PdfStringSyntax.Hex, ObjectContext.UserCreated),
            ["M"] = new Date(signingDate),
        }, this, ObjectContext.UserCreated);

        if (!string.IsNullOrWhiteSpace(options.SignerName))
        {
            signatureDictionary.Set("Name", PdfString.FromTextAuto(options.SignerName, ObjectContext.UserCreated));
        }

        if (!string.IsNullOrWhiteSpace(options.Reason))
        {
            signatureDictionary.Set("Reason", PdfString.FromTextAuto(options.Reason, ObjectContext.UserCreated));
        }

        if (!string.IsNullOrWhiteSpace(options.Location))
        {
            signatureDictionary.Set("Location", PdfString.FromTextAuto(options.Location, ObjectContext.UserCreated));
        }

        if (!string.IsNullOrWhiteSpace(options.ContactInfo))
        {
            signatureDictionary.Set("ContactInfo", PdfString.FromTextAuto(options.ContactInfo, ObjectContext.UserCreated));
        }

        var signatureObject = await Objects.AddAsync(signatureDictionary);
        fieldDictionary.SetValue(signatureObject.Reference);
        Objects.Update(signatureFieldIndirectObject);

        if (options.VisibleAppearance)
        {
            await EnsureVisibleSignatureAppearanceAsync(signatureFieldIndirectObject, fieldDictionary, signingDate, options);
        }

        var documentCatalog = await Objects.GetDocumentCatalogAsync();
        if (await documentCatalog.AcroForm.GetAsync() is InteractiveFormDictionary acroForm)
        {
            acroForm.Set(Constants.DictionaryKeys.InteractiveForm.SigFlags, (Number)3);
            if (documentCatalog.GetAs<IndirectObjectReference>(Constants.DictionaryKeys.DocumentCatalog.AcroForm) is IndirectObjectReference acroFormReference)
            {
                var acroFormObject = await Objects.GetAsync(acroFormReference);
                Objects.Update(acroFormObject);
            }
        }

        _pendingSignature = new PendingPdfSignature(signatureObject, certificate, options);
        _rewriteAllObjects = false;
    }

    private async Task<IndirectObject> EnsureHiddenSignatureFieldAsync(string? fieldName)
    {
        if (await GetPageCountAsync() == 0)
        {
            await AppendPageAsync();
        }

        var trailer = await Objects.GetLatestTrailerDictionaryAsync();
        var catalogObject = await Objects.GetAsync(trailer.Root!);
        var documentCatalog = (DocumentCatalogDictionary)catalogObject.Object;
        var acroFormObject = await EnsureAcroFormObjectAsync(documentCatalog, catalogObject);
        var acroForm = (InteractiveFormDictionary)acroFormObject.Object;

        var page = await GetPageAsync(1);
        var annotations = await page.Dictionary.Annots.GetAsync() ?? new ArrayObject([], ObjectContext.UserCreated);
        var hiddenField = FieldDictionary.FromDictionary(new Dictionary<string, IPdfObject>
        {
            [Constants.DictionaryKeys.Type] = (Name)Constants.DictionaryTypes.Annot,
            [Constants.DictionaryKeys.Subtype] = (Name)AnnotationDictionary.Subtypes.Widget,
            [Constants.DictionaryKeys.Field.FT] = (Name)"Sig",
            [Constants.DictionaryKeys.Field.T] = PdfString.FromTextAuto(
                string.IsNullOrWhiteSpace(fieldName) ? UniqueStringGenerator.Generate() : fieldName,
                ObjectContext.UserCreated),
            [Constants.DictionaryKeys.Annotation.Rect] = Rectangle.FromDimensions(0, 0),
            [Constants.DictionaryKeys.Annotation.P] = page.IndirectObject.Reference,
            [Constants.DictionaryKeys.Annotation.F] = (Number)34
        }, this, ObjectContext.UserCreated);

        var hiddenFieldObject = await Objects.AddAsync(hiddenField);
        (await acroForm.Fields.GetAsync()).Add(hiddenFieldObject.Reference);
        annotations.Add(hiddenFieldObject.Reference);
        page.Dictionary.Set("Annots", annotations);

        Objects.Update(page.IndirectObject);
        Objects.Update(acroFormObject);

        return hiddenFieldObject;
    }

    private async Task<IndirectObject> EnsureAcroFormObjectAsync(
        DocumentCatalogDictionary documentCatalog,
        IndirectObject documentCatalogObject)
    {
        var acroFormReference = documentCatalog.GetAs<IndirectObjectReference>(Constants.DictionaryKeys.DocumentCatalog.AcroForm);
        if (acroFormReference is not null)
        {
            return await Objects.GetAsync(acroFormReference);
        }

        var acroForm = await documentCatalog.AcroForm.GetAsync()
            ?? InteractiveFormDictionary.FromDictionary(
                new Dictionary<string, IPdfObject>
                {
                    [Constants.DictionaryKeys.InteractiveForm.Fields] = new ArrayObject([], ObjectContext.UserCreated)
                },
                this,
                ObjectContext.UserCreated);

        var acroFormObject = await Objects.AddAsync(acroForm);
        documentCatalog.Set(Constants.DictionaryKeys.DocumentCatalog.AcroForm, acroFormObject.Reference);
        Objects.Update(documentCatalogObject);

        return acroFormObject;
    }

    private static PdfSignatureOptions CreateInvisibleSignatureOptions(PdfSignatureOptions? options)
    {
        options ??= new PdfSignatureOptions();

        return new PdfSignatureOptions
        {
            FieldName = options.FieldName,
            VisibleAppearance = false,
            SignatureImageBytes = options.SignatureImageBytes,
            SignerName = options.SignerName,
            Reason = options.Reason,
            Location = options.Location,
            ContactInfo = options.ContactInfo,
            SigningDate = options.SigningDate,
            EstimatedSignatureSizeBytes = options.EstimatedSignatureSizeBytes,
            DigestAlgorithm = options.DigestAlgorithm
        };
    }

    private async Task EnsureVisibleSignatureAppearanceAsync(
        IndirectObject fieldIndirectObject,
        FieldDictionary fieldDictionary,
        DateTimeOffset signingDate,
        PdfSignatureOptions options)
    {
        var fieldBounds = await fieldDictionary.Rect.GetAsync();
        var boundingBox = Rectangle.FromDimensions(fieldBounds.Size.Width, fieldBounds.Size.Height);

        var fontDictionary = new Type1FontDictionary(this, ObjectContext.UserCreated);
        fontDictionary.Set(Constants.DictionaryKeys.Font.BaseFont, (Name)StandardPdfFonts.Helvetica);
        fontDictionary.Set(Constants.DictionaryKeys.Font.Encoding, (Name)Text.Encoding.PDFEncoding.WinAnsi);
        var fontObject = await Objects.AddAsync(fontDictionary);
        var fontResourceName = (Name)"SigFont";

        var resources = new ResourceDictionary(this, ObjectContext.UserCreated);
        await resources.AddFontAsync(fontResourceName, fontObject.Reference, this);

        Rectangle? imageBounds = null;
        var contentStreams = new List<ContentStream>
        {
            BuildSignatureAppearanceBackgroundContentStream(boundingBox)
        };

        if (await TryCreateSignatureAppearanceImageAsync(options, resources, boundingBox) is { } imageAppearance)
        {
            imageBounds = imageAppearance.Bounds;
            contentStreams.Add(new ImageXObjectContentStream(imageAppearance.ResourceName, imageAppearance.Bounds, ObjectContext.UserCreated));
        }

        contentStreams.Add(
            BuildSignatureAppearanceTextContentStream(
                boundingBox,
                fontResourceName,
                options,
                signingDate,
                imageBounds));

        var formDictionary = new Type1FormDictionary(
            this,
            ObjectContext.UserCreated,
            boundingBox,
            resources);

        var appearanceStream = await new ContentStreamFactory(contentStreams).CreateAsync(formDictionary, ObjectContext.UserCreated);
        var appearanceObject = await Objects.AddAsync(appearanceStream);

        fieldDictionary.SetAppearanceDictionary(
            AppearanceDictionary.Create(
                this,
                ObjectContext.UserCreated,
                appearanceObject.Reference));
        Objects.Update(fieldIndirectObject);
    }

    private async Task<SignatureAppearanceImage?> TryCreateSignatureAppearanceImageAsync(
        PdfSignatureOptions options,
        ResourceDictionary resources,
        Rectangle boundingBox)
    {
        if (options.SignatureImageBytes is not { Length: > 0 })
        {
            return null;
        }

        const double inset = 6d;
        var maxImageSide = Math.Min((double)boundingBox.Height - (inset * 2d), (double)boundingBox.Width * 0.3d);
        if (maxImageSide < 24d)
        {
            return null;
        }

        using var imageData = new MemoryStream(options.SignatureImageBytes, writable: false);
        var preparedImage = await ImageXObjectBuilder.CreateAsync(imageData);
        var imageDictionary = CreateImageDictionary(preparedImage);

        if (preparedImage.SoftMask is not null)
        {
            var softMaskDictionary = CreateImageDictionary(preparedImage.SoftMask);
            var softMaskObject = new StreamObject<ImageDictionary>(
                preparedImage.SoftMask.Data,
                softMaskDictionary,
                ObjectContext.UserCreated);
            var softMaskIndirectObject = await Objects.AddAsync(softMaskObject);

            imageDictionary.Set(Constants.DictionaryKeys.Image.SMask, softMaskIndirectObject.Reference);
        }

        var imageXObject = new StreamObject<ImageDictionary>(
            preparedImage.Data,
            imageDictionary,
            ObjectContext.UserCreated);
        var imageObject = await Objects.AddAsync(imageXObject);
        var resourceName = (Name)"SigImage";
        await resources.AddXObjectAsync(resourceName, imageObject.Reference, this);

        var scale = Math.Min(maxImageSide / preparedImage.Width, maxImageSide / preparedImage.Height);
        var renderedWidth = preparedImage.Width * scale;
        var renderedHeight = preparedImage.Height * scale;
        var lowerLeftY = ((double)boundingBox.Height - renderedHeight) / 2d;
        var imageBounds = Rectangle.FromCoordinates(
            new Coordinate(inset, lowerLeftY),
            new Coordinate(inset + renderedWidth, lowerLeftY + renderedHeight));

        return new SignatureAppearanceImage(resourceName, imageBounds);
    }

    private ImageDictionary CreateImageDictionary(PreparedImageXObject preparedImage)
    {
        var filter = preparedImage.FilterName;
        return new ImageDictionary(
            this,
            ObjectContext.UserCreated,
            preparedImage.Width,
            preparedImage.Height,
            preparedImage.ColorSpace,
            preparedImage.BitsPerComponent,
            string.IsNullOrWhiteSpace(filter) ? null : new ShorthandArrayObject([(Name)filter], ObjectContext.UserCreated),
            null);
    }

    private static ContentStream BuildSignatureAppearanceBackgroundContentStream(Rectangle boundingBox)
    {
        var width = (double)boundingBox.Width;
        var height = (double)boundingBox.Height;

        var stream = new ContentStream();

        stream.SetColour(RGBColour.White);
        stream.Operations.Add(new ContentStreamOperation
        {
            Operator = PathConstruction.re,
            Operands = [(Number)0, (Number)0, (Number)width, (Number)height]
        });
        stream.Operations.Add(new ContentStreamOperation { Operator = PathPainting.f });

        stream.SetStrokeColour(new RGBColour(0.75, 0.75, 0.75));
        stream.SetLineWidth(1);
        stream.Operations.Add(new ContentStreamOperation
        {
            Operator = PathConstruction.re,
            Operands = [(Number)0.5, (Number)0.5, (Number)(width - 1), (Number)(height - 1)]
        });
        stream.Operations.Add(new ContentStreamOperation { Operator = PathPainting.S });

        return stream;
    }

    private static ContentStream BuildSignatureAppearanceTextContentStream(
        Rectangle boundingBox,
        Name fontResourceName,
        PdfSignatureOptions options,
        DateTimeOffset signingDate,
        Rectangle? imageBounds)
    {
        var width = (double)boundingBox.Width;
        var height = (double)boundingBox.Height;
        var inset = 6d;

        var lines = new List<string>
        {
            "Digitally signed",
            string.IsNullOrWhiteSpace(options.SignerName) ? "Signer unavailable" : options.SignerName!,
            signingDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz")
        };

        if (!string.IsNullOrWhiteSpace(options.Reason))
        {
            lines.Add(options.Reason!);
        }

        var stream = new ContentStream();
        var textLeft = imageBounds is null
            ? inset
            : Math.Min(width - inset, (double)imageBounds.UpperRight.X + inset);

        stream.BeginTextObject()
            .SetColour(RGBColour.Black)
            .SetTextState(fontResourceName, 9);

        var currentY = height - inset - 10;
        foreach (var line in lines.Where(static x => !string.IsNullOrWhiteSpace(x)))
        {
            if (currentY < inset)
            {
                break;
            }

            stream.SetTextMatrix(1, 0, 0, 1, (Number)textLeft, (Number)currentY)
                .ShowText(PdfString.FromTextAuto(line, ObjectContext.UserCreated));

            currentY -= 11;
        }

        stream.EndTextObject();

        return stream;
    }

    private sealed record SignatureAppearanceImage(Name ResourceName, Rectangle Bounds);

    private async Task WriteDocumentAsync(
        Stream outputStream,
        PdfMetadata metadata,
        EncryptionWritePlan? encryptionWritePlan)
    {
        if (_removeHistoryOnSave)
        {
            await SaveWithoutHistoryAsync(outputStream, metadata, encryptionWritePlan);
            return;
        }

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

    private async Task<IReadOnlyList<int>> NormalizeSelectedPageNumbersAsync(IEnumerable<int> pageNumbers)
    {
        ArgumentNullException.ThrowIfNull(pageNumbers);

        var selectedPageNumbers = pageNumbers.ToList();
        if (selectedPageNumbers.Count == 0)
        {
            throw new ArgumentException("At least one page number must be provided.", nameof(pageNumbers));
        }

        if (selectedPageNumbers.Count != selectedPageNumbers.Distinct().Count())
        {
            throw new ArgumentException("Page numbers must be unique.", nameof(pageNumbers));
        }

        var pageCount = await GetPageCountAsync();
        foreach (var pageNumber in selectedPageNumbers)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumbers));
            if (pageNumber > pageCount)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumbers), $"Page number {pageNumber} must be less than or equal to the total number of pages.");
            }
        }

        return selectedPageNumbers;
    }

    private async Task<Page> AppendCopiedPageAsync(IndirectObject sourcePageIndirectObject, PdfObjectGraphCopier copier)
    {
        ArgumentNullException.ThrowIfNull(sourcePageIndirectObject);
        ArgumentNullException.ThrowIfNull(copier);

        var appendParentIndirectObject = await EnsureAppendLeafAsync();
        var standalonePage = await CreateStandalonePageDictionaryAsync(
            (PageDictionary)sourcePageIndirectObject.Object,
            appendParentIndirectObject.Reference);

        var pageIndirectObject = await Objects.AddAsync(standalonePage);
        copier.RegisterMapping(sourcePageIndirectObject.Reference, pageIndirectObject.Reference);
        pageIndirectObject.Object = await copier.CopyAsync(standalonePage);

        var appendParent = (PageTreeNodeDictionary)appendParentIndirectObject.Object;
        await appendParent.AddChildAsync(pageIndirectObject.Reference);
        Objects.Update(appendParentIndirectObject);
        _appendLeafHint = appendParentIndirectObject.Reference;

        if (await appendParent.Parent.GetRawValueAsync() is IndirectObjectReference)
        {
            await IncrementPageCountAsync(appendParent);
        }

        Objects.PageTree.Reset();

        return new Page(pageIndirectObject, this);
    }

    private static async Task<PageDictionary> CreateStandalonePageDictionaryAsync(
        PageDictionary sourcePage,
        IndirectObjectReference parentReference)
    {
        ArgumentNullException.ThrowIfNull(sourcePage);
        ArgumentNullException.ThrowIfNull(parentReference);

        var standalonePage = new PageDictionary((Syntax.Objects.Dictionaries.Dictionary)sourcePage.Clone());
        standalonePage.SetParent(parentReference);

        standalonePage.Set(
            Constants.DictionaryKeys.PageTree.Resources,
            (Syntax.Objects.Dictionaries.Dictionary)((await sourcePage.Resources.GetAsync())
                ?? throw new InvalidOperationException("Unable to resolve the source page resources.")).Clone());
        standalonePage.Set(
            Constants.DictionaryKeys.PageTree.MediaBox,
            (Syntax.CommonDataStructures.Rectangle)((await sourcePage.MediaBox.GetAsync())
                ?? throw new InvalidOperationException("Unable to resolve the source page media box.")).Clone());

        if (await sourcePage.CropBox.GetAsync() is Syntax.CommonDataStructures.Rectangle cropBox)
        {
            standalonePage.Set(Constants.DictionaryKeys.PageTree.CropBox, (Syntax.CommonDataStructures.Rectangle)cropBox.Clone());
        }
        else
        {
            standalonePage.Unset(Constants.DictionaryKeys.PageTree.CropBox);
        }

        if (await sourcePage.Rotate.GetAsync() is Number rotation)
        {
            standalonePage.Set(Constants.DictionaryKeys.PageTree.Rotate, (Number)rotation.Clone());
        }
        else
        {
            standalonePage.Unset(Constants.DictionaryKeys.PageTree.Rotate);
        }

        standalonePage.Unset(Constants.DictionaryKeys.PageTree.Page.StructParents);

        return standalonePage;
    }

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

    private async Task<IndirectObject> EnsureAppendLeafAsync()
    {
        if (_appendLeafHint != null)
        {
            var hintedLeaf = await Objects.GetAsync(_appendLeafHint);
            var hintedNode = hintedLeaf.Object as PageTreeNodeDictionary;
            if (hintedNode != null && (await hintedNode.Kids.GetAsync()).Count() < PageTreeBranchFactor)
            {
                return hintedLeaf;
            }

            _appendLeafHint = null;
        }

        var rootPageTreeNode = await Objects.PageTree.GetRootPageTreeNodeAsync();

        while (true)
        {
            var leaf = await TryGetAppendLeafAsync(rootPageTreeNode);
            if (leaf != null)
            {
                return leaf;
            }

            rootPageTreeNode = await GrowRootForAppendAsync(rootPageTreeNode);
        }
    }

    private async Task<IndirectObject?> TryGetAppendLeafAsync(IndirectObject nodeIndirectObject)
    {
        var childObjects = await NormalizeAndResolveChildrenAsync(nodeIndirectObject);

        if (childObjects.Count == 0 || childObjects.All(static child => child.Object is PageDictionary))
        {
            return childObjects.Count < PageTreeBranchFactor
                ? nodeIndirectObject
                : null;
        }

        var rightmostChild = childObjects[^1];
        var leaf = await TryGetAppendLeafAsync(rightmostChild);
        if (leaf != null)
        {
            return leaf;
        }

        if (childObjects.Count >= PageTreeBranchFactor)
        {
            return null;
        }

        var newBranch = await CreateEmptyAppendBranchAsync(rightmostChild, nodeIndirectObject.Reference);
        var nodeDictionary = (PageTreeNodeDictionary)nodeIndirectObject.Object;
        var kids = await nodeDictionary.Kids.GetAsync();
        kids.Add(newBranch.Root.Reference);
        Objects.Update(nodeIndirectObject);

        return newBranch.Leaf;
    }

    private async Task<IReadOnlyList<IndirectObject>> NormalizeAndResolveChildrenAsync(IndirectObject nodeIndirectObject)
    {
        var nodeDictionary = (PageTreeNodeDictionary)nodeIndirectObject.Object;
        var kids = await nodeDictionary.Kids.GetAsync();
        var originalRefs = kids.Cast<IndirectObjectReference>().ToList();
        if (originalRefs.Count == 0)
        {
            return [];
        }

        var resolvedChildren = new List<IndirectObject>(originalRefs.Count);
        foreach (var childRef in originalRefs)
        {
            resolvedChildren.Add(await Objects.GetAsync(childRef));
        }

        var normalizedRefs = new List<IndirectObjectReference>(originalRefs.Count);
        var normalizedChildren = new List<IndirectObject>(originalRefs.Count);
        var changed = false;

        for (var index = 0; index < resolvedChildren.Count; index++)
        {
            var child = resolvedChildren[index];
            if (child.Object is not PageDictionary)
            {
                normalizedRefs.Add(child.Reference);
                normalizedChildren.Add(child);
                continue;
            }

            var pageRun = new List<IndirectObject>();
            while (index < resolvedChildren.Count && resolvedChildren[index].Object is PageDictionary)
            {
                pageRun.Add(resolvedChildren[index]);
                index++;
            }

            index--;

            if (pageRun.Count == resolvedChildren.Count && normalizedChildren.Count == 0)
            {
                return resolvedChildren;
            }

            var pageRefs = new ArrayObject(pageRun.Select(static page => (IPdfObject)page.Reference), ObjectContext.UserCreated);
            var leafNode = PageTreeNodeDictionary.CreateNew(pageRefs, this, pageRun.Count);
            leafNode.SetParent(nodeIndirectObject.Reference);
            var leafIndirectObject = await Objects.AddAsync(leafNode);

            foreach (var pageObject in pageRun)
            {
                ((PageDictionary)pageObject.Object).SetParent(leafIndirectObject.Reference);
                Objects.Update(pageObject);
            }

            normalizedRefs.Add(leafIndirectObject.Reference);
            normalizedChildren.Add(leafIndirectObject);
            changed = true;
        }

        if (changed)
        {
            await nodeDictionary.ReplaceAllChildrenAsync(normalizedRefs);
            Objects.Update(nodeIndirectObject);
        }

        return normalizedChildren;
    }

    private async Task<AppendBranch> CreateEmptyAppendBranchAsync(IndirectObject templateChild, IndirectObjectReference parentReference)
    {
        if (templateChild.Object is not PageTreeNodeDictionary templateNode)
        {
            throw new InvalidOperationException("Append branch templates must be page tree nodes.");
        }

        var childObjects = await NormalizeAndResolveChildrenAsync(templateChild);
        if (childObjects.Count == 0 || childObjects.All(static child => child.Object is PageDictionary))
        {
            var leafNode = PageTreeNodeDictionary.CreateNew(new ArrayObject([], ObjectContext.UserCreated), this, 0);
            leafNode.SetParent(parentReference);
            var leafIndirectObject = await Objects.AddAsync(leafNode);

            return new AppendBranch(leafIndirectObject, leafIndirectObject);
        }

        var branchNode = PageTreeNodeDictionary.CreateNew(new ArrayObject([], ObjectContext.UserCreated), this, 0);
        branchNode.SetParent(parentReference);
        var branchIndirectObject = await Objects.AddAsync(branchNode);

        var childBranch = await CreateEmptyAppendBranchAsync(childObjects[^1], branchIndirectObject.Reference);
        var branchKids = await branchNode.Kids.GetAsync();
        branchKids.Add(childBranch.Root.Reference);
        Objects.Update(branchIndirectObject);

        return new AppendBranch(branchIndirectObject, childBranch.Leaf);
    }

    private async Task<IndirectObject> GrowRootForAppendAsync(IndirectObject rootPageTreeNodeIndirectObject)
    {
        var rootPageTreeNode = (PageTreeNodeDictionary)rootPageTreeNodeIndirectObject.Object;
        var childObjects = await NormalizeAndResolveChildrenAsync(rootPageTreeNodeIndirectObject);
        if (childObjects.Count == 0)
        {
            return rootPageTreeNodeIndirectObject;
        }

        var rootPageCount = (int)(await rootPageTreeNode.PageCount.GetAsync());
        var preservedChildren = new ArrayObject(childObjects.Select(static child => (IPdfObject)child.Reference), ObjectContext.UserCreated);
        var wrappedNode = PageTreeNodeDictionary.CreateNew(preservedChildren, this, rootPageCount);
        wrappedNode.SetParent(rootPageTreeNodeIndirectObject.Reference);
        var wrappedIndirectObject = await Objects.AddAsync(wrappedNode);

        foreach (var childObject in childObjects)
        {
            ((PageNode)childObject.Object).SetParent(wrappedIndirectObject.Reference);
            Objects.Update(childObject);
        }

        await rootPageTreeNode.ReplaceAllChildrenAsync([wrappedIndirectObject.Reference]);
        rootPageTreeNode.Set(Constants.DictionaryKeys.PageTree.PageTreeNode.Count, (Number)rootPageCount);
        Objects.Update(rootPageTreeNodeIndirectObject);

        return rootPageTreeNodeIndirectObject;
    }

    private async Task PruneEmptyPageTreeNodesAsync(IndirectObject pageTreeNodeIndirectObject)
    {
        var pageTreeNode = (PageTreeNodeDictionary)pageTreeNodeIndirectObject.Object;
        var kids = await pageTreeNode.Kids.GetAsync();
        if (kids.Any())
        {
            Objects.Update(pageTreeNodeIndirectObject);
            return;
        }

        if (await pageTreeNode.Parent.GetRawValueAsync() is not IndirectObjectReference parentReference)
        {
            Objects.Update(pageTreeNodeIndirectObject);
            return;
        }

        var parentIndirectObject = await Objects.GetAsync(parentReference);
        var parent = (PageTreeNodeDictionary)parentIndirectObject.Object;
        var parentKids = await parent.Kids.GetAsync();
        parentKids.Remove<IndirectObjectReference>(x => x.Id.Index == pageTreeNodeIndirectObject.Id.Index);
        Objects.Update(parentIndirectObject);
        Objects.Delete(pageTreeNodeIndirectObject.Id);

        await PruneEmptyPageTreeNodesAsync(parentIndirectObject);
    }

    private async Task AddWatermarkInternalAsync(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var watermarkFont = new Type1FontDictionary(this, ObjectContext.UserCreated);
        watermarkFont.Set(Constants.DictionaryKeys.Font.BaseFont, (Name)StandardPdfFonts.Helvetica);
        watermarkFont.Set(Constants.DictionaryKeys.Font.Encoding, (Name)Text.Encoding.PDFEncoding.WinAnsi);

        var fontObject = await Objects.AddAsync(watermarkFont);
        var fontResourceName = (Name)UniqueStringGenerator.Generate();

        await foreach (var pageObject in Objects.PageTree.EnumeratePagesAsync())
        {
            var page = new Page(pageObject, this);
            await page.AddWatermarkAsync(text, fontObject.Reference, fontResourceName);
        }
    }

    private sealed record AppendBranch(IndirectObject Root, IndirectObject Leaf);

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
        var objectsByIndex = new List<IndirectObject?> { null };
        await foreach (var obj in Objects)
        {
            EnsureObjectSlotCapacity(objectsByIndex, obj.Id.Index);
            objectsByIndex[obj.Id.Index] = obj;
        }

        var section = new CrossReferenceSection(0, ObjectContext.UserCreated);
        section.Add(CrossReferenceEntry.RootFreeEntry);
        var encryptionObjectId = encryptionWritePlan?.EncryptReference?.Id;

        for (var index = 1; index < objectsByIndex.Count; index++)
        {
            var entry = objectsByIndex[index];
            if (entry is null)
            {
                section.Add(CreateFreeCrossReferenceEntry());
                continue;
            }

            IndirectObject objectToWrite = entry;
            if (encryptionWritePlan != null && (encryptionObjectId is null || encryptionObjectId != entry.Id))
            {
                objectToWrite = _removeEncryptionOnSave
                    ? await EncryptionObjectTransformer.DecryptAsync(entry, encryptionWritePlan.Handler)
                    : await EncryptionObjectTransformer.EncryptAsync(entry, encryptionWritePlan.Handler);
            }

            await objectToWrite.WriteAsync(outputStream);
            section.Add(new CrossReferenceEntry(
                objectToWrite.ByteOffset!.Value,
                objectToWrite.Id.GenerationNumber,
                inUse: true,
                compressed: false,
                ObjectContext.UserCreated));
        }

        var xrefTable = new CrossReferenceTable([section], ObjectContext.UserCreated);
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
                section.Index.Count,
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

    private static void FinalizePendingSignature(MemoryStream stagedOutput, PendingPdfSignature pendingSignature)
    {
        var pdfBytes = stagedOutput.ToArray();
        var (signatureOffset, signatureLength) = LocateSignatureObject(pdfBytes, pendingSignature.SignatureObject.Id);
        var signatureSection = pdfBytes.AsSpan(signatureOffset, signatureLength);

        var contentsMarker = "/Contents <"u8;
        var contentsMarkerIndex = signatureSection.IndexOf(contentsMarker);
        if (contentsMarkerIndex < 0)
        {
            throw new InvalidOperationException("Unable to locate the signature contents placeholder.");
        }

        var contentsHexStart = signatureOffset + contentsMarkerIndex + contentsMarker.Length;
        var contentsHexEndRelative = pdfBytes.AsSpan(contentsHexStart).IndexOf((byte)'>');
        if (contentsHexEndRelative < 0)
        {
            throw new InvalidOperationException("Unable to locate the end of the signature contents placeholder.");
        }

        var contentsHexEnd = contentsHexStart + contentsHexEndRelative;
        var contentsValueStart = contentsHexStart - 1;
        var contentsValueEnd = contentsHexEnd + 1;

        var byteRangeMarker = "/ByteRange "u8;
        var byteRangeMarkerIndex = signatureSection.IndexOf(byteRangeMarker);
        if (byteRangeMarkerIndex < 0)
        {
            throw new InvalidOperationException("Unable to locate the signature ByteRange placeholder.");
        }

        var byteRangeStart = signatureOffset + byteRangeMarkerIndex + byteRangeMarker.Length;
        var byteRangeEndRelative = pdfBytes.AsSpan(byteRangeStart).IndexOf((byte)']');
        if (byteRangeEndRelative < 0)
        {
            throw new InvalidOperationException("Unable to locate the end of the signature ByteRange placeholder.");
        }

        var byteRangeEnd = byteRangeStart + byteRangeEndRelative;
        var range1Length = contentsValueStart;
        var range2Start = contentsValueEnd;
        var range2Length = pdfBytes.Length - range2Start;

        var byteRangeText = $"[{0:D10} {range1Length:D10} {range2Start:D10} {range2Length:D10}]";
        Encoding.ASCII.GetBytes(byteRangeText, pdfBytes.AsSpan(byteRangeStart, byteRangeEnd - byteRangeStart + 1));

        var signedContent = new byte[range1Length + range2Length];
        Buffer.BlockCopy(pdfBytes, 0, signedContent, 0, range1Length);
        Buffer.BlockCopy(pdfBytes, range2Start, signedContent, range1Length, range2Length);

        var cms = new SignedCms(new ContentInfo(signedContent), detached: true);
        var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, pendingSignature.Certificate)
        {
            DigestAlgorithm = ResolveDigestOid(pendingSignature.Options.DigestAlgorithm)
        };
        cms.ComputeSignature(signer, silent: true);
        var signatureBytes = cms.Encode();

        var placeholderHexLength = contentsHexEnd - contentsHexStart;
        var signatureHexLength = signatureBytes.Length * 2;
        if (signatureHexLength > placeholderHexLength)
        {
            throw new InvalidOperationException(
                $"The generated signature requires {signatureBytes.Length} bytes, which exceeds the reserved buffer of {placeholderHexLength / 2} bytes.");
        }

        var hexBuffer = pdfBytes.AsSpan(contentsHexStart, placeholderHexLength);
        WriteHex(signatureBytes, hexBuffer);
        hexBuffer[signatureHexLength..].Fill((byte)'0');

        stagedOutput.Position = 0;
        stagedOutput.SetLength(0);
        stagedOutput.Write(pdfBytes);
        stagedOutput.Position = 0;
    }

    private static (int Offset, int Length) LocateSignatureObject(byte[] pdfBytes, IndirectObjectId id)
    {
        var header = Encoding.ASCII.GetBytes($"{id.Index} {id.GenerationNumber} obj");
        var objectStart = pdfBytes.AsSpan().IndexOf(header);
        if (objectStart < 0)
        {
            throw new InvalidOperationException("Unable to locate the signature object in the saved PDF.");
        }

        var objectEndRelative = pdfBytes.AsSpan(objectStart).IndexOf("endobj"u8);
        if (objectEndRelative < 0)
        {
            throw new InvalidOperationException("Unable to locate the end of the signature object in the saved PDF.");
        }

        return (objectStart, objectEndRelative);
    }

    private static Oid ResolveDigestOid(HashAlgorithmName digestAlgorithm)
        => digestAlgorithm.Name switch
        {
            "SHA384" => new Oid("2.16.840.1.101.3.4.2.2"),
            "SHA512" => new Oid("2.16.840.1.101.3.4.2.3"),
            _ => new Oid("2.16.840.1.101.3.4.2.1"),
        };

    private static void WriteHex(ReadOnlySpan<byte> input, Span<byte> destination)
    {
        const string hex = "0123456789ABCDEF";
        for (var i = 0; i < input.Length; i++)
        {
            var value = input[i];
            destination[i * 2] = (byte)hex[value >> 4];
            destination[i * 2 + 1] = (byte)hex[value & 0x0F];
        }
    }

    private static void EnsureObjectSlotCapacity(List<IndirectObject?> objectsByIndex, int index)
    {
        while (objectsByIndex.Count <= index)
        {
            objectsByIndex.Add(null);
        }
    }

    private static CrossReferenceEntry CreateFreeCrossReferenceEntry()
        => new(
            0,
            0,
            inUse: false,
            compressed: false,
            ObjectContext.UserCreated);

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
