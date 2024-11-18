using ZingPDF.Elements;
using ZingPDF.Elements.Forms;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Linearization;
using ZingPDF.Parsing;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF;

/// <summary>
/// Represents a PDF document which is not editable.
/// </summary>
/// <remarks>
/// This class is disposable. The underlying <see cref="Stream"/> will remain open until the instance is disposed.<para></para>
/// </remarks>
public class ReadOnlyPdf : IPdf, IDisposable
{
    private readonly Stream _pdfInputStream;
    private readonly LinearizationParameterDictionary? _linearizationDictionary;

    /// <summary>
    /// Internal constructor for creating a <see cref="ReadOnlyPdf"/> from its constituent parts.
    /// </summary>
    public ReadOnlyPdf(
        Stream pdfInputStream,
        DocumentCatalogDictionary documentCatalog,
        Trailer? trailer,
        IndirectObject? xrefStream,
        ReadOnlyIndirectObjectDictionary indirectObjectDictionary,
        LinearizationParameterDictionary? linearizationDictionary
        )
    {
        _pdfInputStream = pdfInputStream ?? throw new ArgumentNullException(nameof(pdfInputStream));
        DocumentCatalog = documentCatalog ?? throw new ArgumentNullException(nameof(documentCatalog));
        Trailer = trailer;
        CrossReferenceStream = xrefStream;
        IndirectObjects = indirectObjectDictionary ?? throw new ArgumentNullException(nameof(indirectObjectDictionary));
        _linearizationDictionary = linearizationDictionary;

        PageTree = new PageTree(indirectObjectDictionary, DocumentCatalog.Pages);
    }

    #region IPdf

    public IIndirectObjectDictionary IndirectObjects { get; }
    public Trailer? Trailer { get; }
    public IndirectObject? CrossReferenceStream { get; }
    public DocumentCatalogDictionary DocumentCatalog { get; }
    public PageTree PageTree { get; }

    public ITrailerDictionary TrailerDictionary => Trailer?.Dictionary
        ?? (CrossReferenceStream?.Object as IStreamObject<IStreamDictionary>)?.Dictionary as ITrailerDictionary
        ?? throw new ParserException("Unable to find trailer dictionary");

    async Task<IList<IndirectObject>> IPdf.GetAllPagesAsync() => await PageTree.GetPagesAsync();

    public async Task<Page> GetPageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));

        var pageIndirectObject = (await PageTree.GetPagesAsync())[pageNumber - 1];

        return pageIndirectObject == null
            ? throw new InvalidOperationException()
            : new Page(pageIndirectObject, IndirectObjects);
    }

    public Task<int> GetPageCountAsync() => PageTree.GetPageCountAsync();

    // TODO: duplicate logic in Pdf. See if we can share it.
    public Form? GetForm()
    {
        if (DocumentCatalog.AcroForm is null)
        {
            return null;
        }

        return new Form(DocumentCatalog.AcroForm, IndirectObjects);
    }

    public async Task SaveAsync(Stream outputStream, PdfSaveOptions? saveOptions)
    {
        _pdfInputStream.Position = 0;
        await _pdfInputStream.CopyToAsync(outputStream);
    }

    #endregion

    // TODO: move this to the interface
    /// <summary>
    /// PDF is linearized if there is a linearization dictionary, AND
    /// the length value (L) is identical to the length of the stream.
    /// A mismatch indicates the file has had at least one incremental update applied,
    /// and should be considered to not be linearized.
    /// </summary>
    internal bool Linearized => _linearizationDictionary != null && _linearizationDictionary.L == _pdfInputStream.Length;

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
