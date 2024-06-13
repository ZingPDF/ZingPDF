using ZingPDF.ObjectModel.DocumentStructure;
using ZingPDF.ObjectModel.FileStructure.Trailer;

namespace ZingPDF.Parsing;

public interface IPdf
{
    Task<int> GetPageCountAsync();
}

public interface IReadOnlyPdf : IPdf
{
    //ReadOnlyIndirectObjectDictionary IndirectObjects { get; }
    //DocumentCatalogDictionary DocumentCatalog { get; }
    //ITrailerDictionary TrailerDictionary { get; }
}
