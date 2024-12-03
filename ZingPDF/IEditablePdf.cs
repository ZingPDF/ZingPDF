using ZingPDF.Elements;
using ZingPDF.Syntax.DocumentStructure.PageTree;

namespace ZingPDF;

public interface IEditablePdf : IPdf
{
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
