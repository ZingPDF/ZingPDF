namespace ZingPDF;

/// <summary>
/// Selects the Standard security handler encryption algorithm used when saving a PDF.
/// </summary>
public enum PdfEncryptionAlgorithm
{
    /// <summary>
    /// RC4 with a 128-bit file key.
    /// </summary>
    Rc4_128 = 0,

    /// <summary>
    /// AES-128 using the Standard security handler crypt filters (<c>V=4</c>, <c>R=4</c>).
    /// </summary>
    Aes128 = 1,

    /// <summary>
    /// AES-256 using the Standard security handler crypt filters (<c>V=5</c>, <c>R=6</c>).
    /// </summary>
    Aes256 = 2,
}
