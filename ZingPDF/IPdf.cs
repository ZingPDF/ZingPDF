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

    /// <summary>
    /// Saves the PDF using standard password protection.
    /// </summary>
    /// <remarks>
    /// If the source PDF is already encrypted, authenticate it first so the document can be rewritten.
    /// The current implementation writes Standard security handler encryption using RC4 (V=2, R=3).
    /// </remarks>
    Task EncryptAsync(string userPassword, string? ownerPassword = null);

    /// <summary>
    /// Authenticates with the supplied password and saves the PDF without encryption.
    /// </summary>
    Task DecryptAsync(string password);

    Task AppendPdfAsync(Stream stream);

    Task SaveAsync(Stream outputStream);
}
