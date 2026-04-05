//namespace ZingPDF.Syntax.Encryption;

//internal readonly struct DecryptionPlan
//{
//    public readonly SecurityContext? Ctx; // null => clear
//    public readonly int ObjNum;
//    public readonly int GenNum;
//    public readonly CryptoChoice Choice;  // Identity/RC4/AES-128/AES-256
//    public readonly bool IsEncrypted;     // after all exceptions applied

//    public bool IsIdentity => Ctx is null || !IsEncrypted || Choice.Algorithm == CryptoAlgorithm.Identity;
//}
