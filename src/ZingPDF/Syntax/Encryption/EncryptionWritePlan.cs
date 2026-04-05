using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Syntax.Encryption;

internal sealed record PdfEncryptionOptions(
    string UserPassword,
    string OwnerPassword,
    PdfEncryptionAlgorithm Algorithm,
    int Permissions = unchecked((int)0xFFFFFFFC),
    int KeyLengthBits = 128,
    bool EncryptMetadata = true)
{
    public static PdfEncryptionOptions Create(
        string userPassword,
        string ownerPassword,
        PdfEncryptionAlgorithm algorithm,
        int permissions = unchecked((int)0xFFFFFFFC),
        bool encryptMetadata = true)
        => algorithm switch
        {
            PdfEncryptionAlgorithm.Rc4_128 => new(userPassword, ownerPassword, algorithm, permissions, 128, encryptMetadata),
            PdfEncryptionAlgorithm.Aes128 => new(userPassword, ownerPassword, algorithm, permissions, 128, encryptMetadata),
            PdfEncryptionAlgorithm.Aes256 => new(userPassword, ownerPassword, algorithm, permissions, 256, encryptMetadata),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
        };
}

internal sealed record EncryptionWritePlan(
    StandardSecurityHandler Handler,
    IndirectObjectReference? EncryptReference = null,
    PdfString? OriginalFileId = null);
