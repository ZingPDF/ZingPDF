using ZingPDF.Elements;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Elements.Forms;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

public interface IPdf
{
    Stream Data { get; }
    IPdfObjectCollection Objects { get; }

    /// <summary>
    /// Unlocks the PDF for reading and/or writing. The password can be a user password or an owner password.
    /// </summary>
    Task AuthenticateAsync(string password);

    Task<IList<IndirectObject>> GetAllPagesAsync();
    Task<Page> GetPageAsync(int pageNumber);
    Task<int> GetPageCountAsync();

    Task<Form?> GetFormAsync();

    Task<Page> AppendPageAsync(Action<PageDictionary.PageCreationOptions>? configureOptions = null);
    Task<Page> InsertPageAsync(int pageNumber, Action<PageDictionary.PageCreationOptions>? configureOptions = null);
    Task DeletePageAsync(int pageNumber);
    Task SetRotationAsync(Rotation rotation);

    Task<IEnumerable<ExtractedText>> ExtractTextAsync();

    Task AddWatermarkAsync(string text);

    void Compress(int dpi, int quality);

    /// <summary>
    /// Removes compression filters from all objects in the PDF.
    /// </summary>
    /// <remarks>
    /// This applies an incremental update to the PDF with all objects decompressed. This can add significant size to the PDF.
    /// Typically, this is used to make the source code of a PDF more readable.
    /// </remarks>
    Task DecompressAsync();

    void Encrypt();
    void Decrypt();

    Task AppendPdfAsync(Stream stream);

    Task SaveAsync(Stream outputStream);
}
