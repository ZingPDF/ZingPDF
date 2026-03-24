using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Syntax.Encryption;

internal sealed record PdfEncryptionOptions(
    string UserPassword,
    string OwnerPassword,
    int Permissions = unchecked((int)0xFFFFFFFC),
    int KeyLengthBits = 128,
    bool EncryptMetadata = true);

internal sealed record EncryptionWritePlan(
    StandardSecurityHandler Handler,
    IndirectObjectReference? EncryptReference = null,
    PdfString? OriginalFileId = null);
