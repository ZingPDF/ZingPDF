namespace ZingPDF.Syntax.Encryption;

internal interface ISecurityHandler
{
    byte[] Encrypt(byte[] data);
    byte[] Decrypt(byte[] data);
}
