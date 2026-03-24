using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax;

namespace ZingPDF.Syntax.Encryption;

internal interface IPdfEncryptionProvider
{
    Task AuthenticateAsync(string password);

    Task<byte[]> DecryptObjectBytesAsync(ObjectContext context, byte[] data, IStreamDictionary? streamDictionary);

    Task<EncryptionWritePlan?> CreateWritePlanAsync();
}
