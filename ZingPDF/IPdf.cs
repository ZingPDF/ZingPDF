using ZingPDF.Elements;
using ZingPDF.Elements.Forms;
using ZingPDF.Linearization;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

public interface IPdf2
{
    Task<IList<IndirectObject>> GetAllPagesAsync();
    Task<Page> GetPageAsync(int pageNumber);
    Task<int> GetPageCountAsync();

    Form? GetForm();

    Task<Page> AppendPageAsync(Action<PageDictionary.PageCreationOptions>? configureOptions = null);
    Task<Page> InsertPageAsync(int pageNumber, Action<PageDictionary.PageCreationOptions>? configureOptions = null);
    Task DeletePageAsync(int pageNumber);
    Task SetRotationAsync(Rotation rotation);

    void AddWatermark();
    void Compress(int dpi, int quality);
    void Encrypt();
    void Decrypt();
    void Sign();
    Task AppendPdfAsync(Stream stream);

    Task SaveAsync(Stream stream, PdfSaveOptions? saveOptions);
}

public interface IPdf
{
    bool Linearized { get; }
    bool Encrypted { get; }

    IIndirectObjectDictionary IndirectObjects { get; }
    Trailer? Trailer { get; }
    IndirectObject? CrossReferenceStream { get; }
    DocumentCatalogDictionary DocumentCatalog { get; }
    LinearizationParameterDictionary? LinearizationDictionary { get; }
    PageTree PageTree { get; }

    ITrailerDictionary TrailerDictionary { get; }

    Task<IList<IndirectObject>> GetAllPagesAsync();
    Task<Page> GetPageAsync(int pageNumber);
    Task<int> GetPageCountAsync();

    Form? GetForm();
}
