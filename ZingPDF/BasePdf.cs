using ZingPDF.Elements;
using ZingPDF.Elements.Forms;
using ZingPDF.Linearization;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF;

public abstract class BasePdf : IPdf, IDisposable
{
    protected Form? _form;
    protected readonly Stream _pdfInputStream;

    protected BasePdf(
        Stream pdfInputStream,
        DocumentCatalogDictionary documentCatalog,
        Trailer? trailer,
        IndirectObject? xrefStream,
        IIndirectObjectDictionary indirectObjectDictionary,
        LinearizationParameterDictionary? linearizationDictionary
        )
    {
        _pdfInputStream = pdfInputStream ?? throw new ArgumentNullException(nameof(pdfInputStream));
        DocumentCatalog = documentCatalog ?? throw new ArgumentNullException(nameof(documentCatalog));
        Trailer = trailer;
        CrossReferenceStream = xrefStream;
        IndirectObjects = indirectObjectDictionary ?? throw new ArgumentNullException(nameof(indirectObjectDictionary));
        LinearizationDictionary = linearizationDictionary;

        PageTree = new PageTree(indirectObjectDictionary, DocumentCatalog.Pages);
    }

    // PDF is linearized if there is a linearization dictionary, AND
    // the length value (L) is identical to the length of the stream.
    // A mismatch indicates the file has had at least one incremental update applied,
    // and should be considered to not be linearized.
    public bool Linearized => LinearizationDictionary != null && LinearizationDictionary.L == _pdfInputStream.Length;

    public IIndirectObjectDictionary IndirectObjects { get; }
    public Trailer? Trailer { get; }
    public IndirectObject? CrossReferenceStream { get; }
    public DocumentCatalogDictionary DocumentCatalog { get; }
    public LinearizationParameterDictionary? LinearizationDictionary { get; }
    public PageTree PageTree { get; }

    public ITrailerDictionary TrailerDictionary => Trailer?.Dictionary
        ?? (CrossReferenceStream?.Object as StreamObject<IStreamDictionary>)?.Dictionary as ITrailerDictionary
        ?? throw new ParserException("Unable to find trailer dictionary");

    public Task<IList<IndirectObject>> GetAllPagesAsync() => PageTree.GetPagesAsync();

    public Form? GetForm()
    {
        if (DocumentCatalog.AcroForm is null)
        {
            return null;
        }

        _form = new Form(DocumentCatalog.AcroForm, IndirectObjects);

        return _form;
    }

    public async Task<Page> GetPageAsync(int pageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));

        var pageIndirectObject = (await PageTree.GetPagesAsync())[pageNumber - 1];

        return pageIndirectObject == null
            ? throw new InvalidOperationException()
            : new Page(pageIndirectObject, IndirectObjects);
    }

    public Task<int> GetPageCountAsync() => PageTree.GetPageCountAsync();

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
