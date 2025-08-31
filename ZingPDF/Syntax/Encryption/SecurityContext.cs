//using ZingPDF.Syntax.Objects.Streams;

//namespace ZingPDF.Syntax.Encryption;

//internal sealed class SecurityContext
//{
//    // Parsed from /Encrypt and trailer/ID
//    public int V { get; }
//    public int R { get; }
//    public int LengthBits { get; }
//    public int P { get; }
//    public bool EncryptMetadata { get; }
//    public string? DefaultStmF { get; }   // e.g., "StdCF"
//    public string? DefaultStrF { get; }
//    public string? EFF { get; }           // embedded files default
//    public IReadOnlyDictionary<string, CryptFilter> CF { get; }

//    public byte[] FileID0 { get; }        // trailer /ID[0]
//    public byte[] U { get; }              // or UE (R5/R6)
//    public byte[] O { get; }              // or OE (R5/R6)
//    public byte[]? Perms { get; }         // R5/R6
//    public PermissionsFlags Permissions { get; } // decoded view of P

//    // Derived once after password accepted
//    public byte[] FileKey { get; }        // symmetric doc key (R2–R4) or derived per R5/R6
//    public int FileKeyLen { get; }

//    // Helper: maps an object “purpose” to a crypt choice (Identity/RC4/AES-128/AES-256)
//    public CryptoChoice ResolveChoice(ObjectPurpose purpose, StreamDictionary? stmDict, bool isMetadataStream);
//}
