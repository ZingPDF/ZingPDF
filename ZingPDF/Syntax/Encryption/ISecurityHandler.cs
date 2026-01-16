using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.Encryption;

internal interface ISecurityHandler
{
    bool IsAuthenticated { get; }

    bool TryAuthenticate(string password);

    bool ShouldDecrypt(IndirectObjectId objectId, IStreamDictionary? streamDictionary);

    byte[] Encrypt(IndirectObjectId objectId, byte[] data);

    byte[] Decrypt(IndirectObjectId objectId, byte[] data);
}
