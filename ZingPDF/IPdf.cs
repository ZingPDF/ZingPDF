using ZingPDF.Elements;
using ZingPDF.Elements.Forms;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

public interface IPdf
{
    IIndirectObjectDictionary IndirectObjects { get; }
    Trailer? Trailer { get; }
    IndirectObject? CrossReferenceStream { get; }
    DocumentCatalogDictionary DocumentCatalog { get; }
    PageTree PageTree { get; }

    ITrailerDictionary TrailerDictionary { get; }

    Task<IList<IndirectObject>> GetAllPagesAsync();
    Task<Page> GetPageAsync(int pageNumber);
    Task<int> GetPageCountAsync();

    Form? GetForm();

    Task SaveAsync(Stream stream, PdfSaveOptions? saveOptions);
}
