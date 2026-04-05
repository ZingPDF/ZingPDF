using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.Encryption;

internal sealed class NoOpPdfEncryptionProvider : IPdfEncryptionProvider
{
    public static NoOpPdfEncryptionProvider Instance { get; } = new();

    private NoOpPdfEncryptionProvider()
    {
    }

    public Task AuthenticateAsync(string password)
        => Task.CompletedTask;

    public Task<byte[]> DecryptObjectBytesAsync(ObjectContext context, byte[] data, IStreamDictionary? streamDictionary)
        => Task.FromResult(data);

    public Task<EncryptionWritePlan?> CreateWritePlanAsync(PdfEncryptionOptions? encryptionOptions = null)
        => Task.FromResult<EncryptionWritePlan?>(null);
}
