using Nito.AsyncEx;
using ZingPDF.Extensions;
using ZingPDF.Linearization;
using ZingPDF.ObjectModel.DocumentStructure;
using ZingPDF.ObjectModel.DocumentStructure.PageTree;
using ZingPDF.ObjectModel.FileStructure.Trailer;
using ZingPDF.ObjectModel.Objects.IndirectObjects;
using ZingPDF.ObjectModel.Objects.Streams;
using ZingPDF.Parsing;
using ZingPDF.Parsing.Parsers;

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

    private readonly AsyncLazy<PageTreeNodeDictionary> _rootPageTreeNode;
    private readonly AsyncLazy<List<IndirectObject>> _pages;

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

        _rootPageTreeNode = new AsyncLazy<PageTreeNodeDictionary>(async () =>
        {
            return await IndirectObjects.GetAsync<PageTreeNodeDictionary>(documentCatalog.Pages)
                ?? throw new InvalidPdfException("Unable to find root page tree node");
        });

        _pages = new AsyncLazy<List<IndirectObject>>(async () =>
        {
            var rootPageTreeNode = await _rootPageTreeNode;

            return await rootPageTreeNode.GetSubPagesAsync(IndirectObjects);
        });
    }

    #region IPdf

    public IIndirectObjectDictionary IndirectObjects { get; }
    public Trailer? Trailer { get; }
    public IndirectObject? CrossReferenceStream { get; }
    public DocumentCatalogDictionary DocumentCatalog { get; }

    public ITrailerDictionary TrailerDictionary => Trailer?.Dictionary
        ?? CrossReferenceStream?.Get<IStreamObject<IStreamDictionary>>().Dictionary as ITrailerDictionary
        ?? throw new ParserException("Unable to find trailer dictionary");

    public async Task<IndirectObject> GetPageAsync(int pageNumber)
    {
        return (await _pages)[pageNumber - 1];
    }

    public async Task<int> GetPageCountAsync()
    {
        return (await _rootPageTreeNode).PageCount;
    }

    public async Task SaveAsync(Stream outputStream, PdfSaveOptions? saveOptions)
    {
        _pdfInputStream.Position = 0;
        await _pdfInputStream.CopyToAsync(outputStream);
    }

    #endregion

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
