using System.Security.Cryptography.X509Certificates;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Signing;

internal sealed record PendingPdfSignature(
    IndirectObject SignatureObject,
    X509Certificate2 Certificate,
    PdfSignatureOptions Options);
