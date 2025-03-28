using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Encryption;

public class EncryptionDictionary : Dictionary
{
    public EncryptionDictionary(Dictionary dictionary)
        : base(dictionary) { }

    protected EncryptionDictionary(IEnumerable<KeyValuePair<Name, IPdfObject>> dictionary, IPdfEditor pdfEditor)
        : base(dictionary, pdfEditor) { }

    /// <summary>
    /// <para>
    /// (Required) The name of the preferred security handler for this document. It shall be the 
    /// name of the security handler that was used to encrypt the document. If SubFilter is not present, 
    /// only this security handler shall be used when opening the document. If it is present, a PDF 
    /// processor can use any security handler that implements the format specified by SubFilter.
    /// </para>
    /// <para>
    /// Standard shall be the name of the built-in password-based security handler. Names for other 
    /// security handlers may be registered by using the procedure described in Annex E, "Extending PDF".
    /// </para>
    /// </summary>
    public Name Filter => GetAs<Name>(Constants.DictionaryKeys.Encryption.Filter)!;

    /// <summary>
    /// (Optional; PDF 1.3) A name that completely specifies the format and interpretation of the contents 
    /// of the encryption dictionary. It allows security handlers other than the one specified by Filter 
    /// to decrypt the document. If this entry is absent, other security handlers shall not decrypt the document.
    /// </summary>
    public Name? SubFilter => GetAs<Name>(Constants.DictionaryKeys.Encryption.SubFilter);

    /// <summary>
    /// <para>
    /// (Required) A code specifying the algorithm to be used in encrypting and decrypting the document:
    /// </para>
    /// <para>
    /// 0 An algorithm that is undocumented.This value shall not be used.
    /// </para>
    /// <para>
    /// 1 (Deprecated in PDF 2.0) Indicates the use of 7.6.3.2, "Algorithm 1: Encryption of data using 
    /// the RC4 or AES algorithms" (deprecated in PDF 2.0) with a file encryption key length of 40 bits; see 
    /// below.
    /// </para>
    /// <para>
    /// 2 (PDF 1.4; deprecated in PDF 2.0) Indicates the use of 7.6.3.2, "Algorithm 1: Encryption of data 
    /// using the RC4 or AES algorithms" (deprecated in PDF 2.0) but permitting file encryption key lengths 
    /// greater than 40 bits.
    /// </para>
    /// <para>
    /// 3 (PDF 1.4; deprecated in PDF 2.0) An unpublished algorithm that permits file encryption key lengths 
    /// ranging from 40 to 128 bits.This value shall not appear in a conforming PDF file.
    /// </para>
    /// <para>
    /// 4 (PDF 1.5; deprecated in PDF 2.0) The security handler defines the use of encryption and decryption 
    /// in the document, using the rules specified by the CF, StmF, and StrF entries using 7.6.3.2, "Algorithm 1: 
    /// Encryption of data using the RC4 or AES algorithms" (deprecated in PDF 2.0) with a file encryption key length 
    /// of 128 bits.
    /// </para>
    /// <para>
    /// 5 (PDF 2.0) The security handler defines the use of encryption and decryption in the document, using 
    /// the rules specified by the CF, StmF, StrF and EFF entries using 7.6.3.3, "Algorithm 1.A: Encryption of data 
    /// using the AES algorithms" with a file encryption key length of 256 bits.
    /// </para>
    /// </summary>
    public Number V => GetAs<Number>(Constants.DictionaryKeys.Encryption.V)!;

    /// <summary>
    /// (Optional; PDF 1.4; only if V is 2 or 3; deprecated in PDF 2.0) The length of the file encryption key, 
    /// in bits. The value shall be a multiple of 8, in the range 40 to 128. Default value: 40.
    /// </summary>
    public Number? Length => GetAs<Number>(Constants.DictionaryKeys.Encryption.Length);

    /// <summary>
    /// <para>
    /// (Optional; meaningful only when the value of V is 4 (PDF 1.5) or 5 (PDF 2.0)) A dictionary whose 
    /// keys shall be crypt filter names and whose values shall be the corresponding crypt filter dictionaries 
    /// (see "Table 25 — Entries common to all crypt filter dictionaries"). Every crypt filter used in the document 
    /// shall have an entry in this dictionary, except for the standard crypt filter names (see "Table 26 — Standard 
    /// crypt filter names").
    /// </para>
    /// <para>
    /// Any keys in the CF dictionary that are listed in "Table 26 — Standard crypt filter names" shall be 
    /// ignored by a PDF processor. Instead, the PDF processor shall use properties of the respective standard 
    /// crypt filters.
    /// </para>
    /// </summary>
    public Dictionary? CF => GetAs<Dictionary>(Constants.DictionaryKeys.Encryption.CF);

    /// <summary>
    /// <para>(Optional; meaningful only when the value of V is 4 (PDF 1.5) or 5 (PDF 2.0)) The name of the crypt 
    /// filter that shall be used by default when decrypting streams. The name shall be a key in the CF dictionary 
    /// or a standard crypt filter name specified in "Table 26 — Standard crypt filter names". All streams in the 
    /// document, except for cross-reference streams (see 7.5.8, "Cross-reference streams") or streams that have a 
    /// Crypt entry in their Filter array (see "Table 6 — Standard filters"), shall be decrypted by the security 
    /// handler, using this crypt filter.</para>
    /// <para>Default value: Identity.</para>
    /// </summary>
    public Name? StmF => GetAs<Name>(Constants.DictionaryKeys.Encryption.StmF);

    /// <summary>
    /// <para>(Optional; meaningful only when the value of V is 4 (PDF 1.5) or 5 (PDF 2.0)) The name of the crypt 
    /// filter that shall be used when decrypting all strings in the document. The name shall be a key in the CF 
    /// dictionary or a standard crypt filter name specified in "Table 26 — Standard crypt filter names".</para>
    /// <para>Default value: Identity.</para>
    /// </summary>
    public Name? StrF => GetAs<Name>(Constants.DictionaryKeys.Encryption.StrF);

    /// <summary>
    /// <para>(Optional; meaningful only when the value of V is 4 (PDF 1.6) or 5 (PDF 2.0)) The name of the crypt 
    /// filter that shall be used when encrypting embedded file streams that do not have their own crypt filter 
    /// specifier; it shall correspond to a key in the CF dictionary or a standard crypt filter name specified in 
    /// "Table 26 — Standard crypt filter names".</para>
    /// <para>This entry shall be provided by the security handler. PDF writers shall respect this value when 
    /// encrypting embedded files, except for embedded file streams that have their own crypt filter specifier. 
    /// If this entry is not present, and the embedded file stream does not contain a crypt filter specifier, the 
    /// stream shall be encrypted using the default stream crypt filter specified by StmF.</para>
    /// </summary>
    public Name? EFF => GetAs<Name>(Constants.DictionaryKeys.Encryption.EFF);
}
