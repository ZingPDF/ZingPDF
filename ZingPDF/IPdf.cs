using ZingPDF.Elements;
using ZingPDF.Elements.Forms;
using ZingPDF.Linearization;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

public interface IPdf
{
    bool Linearized { get; }

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
