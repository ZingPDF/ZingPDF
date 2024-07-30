using ZingPDF.Elements;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

public interface IPdf
{
    IIndirectObjectDictionary IndirectObjects { get; }
    Trailer? Trailer { get; }
    IndirectObject? CrossReferenceStream { get; }
    DocumentCatalogDictionary DocumentCatalog { get; }

    ITrailerDictionary TrailerDictionary { get; }

    Task<Page> GetPageAsync(int pageNumber);
    Task<int> GetPageCountAsync();

    Task SaveAsync(Stream stream, PdfSaveOptions? saveOptions);
}
