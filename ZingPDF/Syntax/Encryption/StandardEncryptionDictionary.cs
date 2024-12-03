using ZingPDF.Syntax.Objects;

namespace ZingPDF.Syntax.Encryption
{
    internal class StandardEncryptionDictionary : EncryptionDictionary
    {
        private StandardEncryptionDictionary(IEnumerable<KeyValuePair<Name, IPdfObject>> dictionary)
            : base(dictionary)
        {
        }

        /// <summary>
        /// <para>(Required) A number specifying which revision of the standard security handler shall 
        /// be used to interpret this dictionary:</para>
        /// <para>2 (Deprecated in PDF 2.0) if the document is encrypted with a V value less than 2 
        /// (see "Table 20 — Entries common to all encryption dictionaries") and does not have any 
        /// of the access permissions set to 0 (by means of the P entry, below) that are designated 
        /// "Security handlers of revision 3 or greater" in "Table 22 — Standard security handler user 
        /// access permissions".</para>
        /// <para>3 (Deprecated in PDF 2.0) if the document is encrypted with a V value of 2 or 3, or 
        /// has any "Security handlers of revision 3 or greater" access permissions set to 0.</para>
        /// <para>4 (Deprecated in PDF 2.0) if the document is encrypted with a V value of 4.</para>
        /// <para>5 (PDF 2.0; deprecated in PDF 2.0) Shall not be used. This value was used by a 
        /// deprecated proprietary Adobe extension.</para>
        /// <para>6 (PDF 2.0) if the document is encrypted with a V value of 5.</para>
        /// </summary>
        public Integer R => Get<Integer>(Constants.DictionaryKeys.Encryption.Standard.R)!;

        // TODO: this is supposed to be a byte string.
        /// <summary>
        /// <para>(Required) A byte string, 32 bytes long if the value of R is 4 or less and 48 bytes 
        /// long if the value of R is 6, based on both the owne r and user passwords, that shall be used 
        /// in computing the file encryption key and in determining whether a valid owner password was 
        /// entered.</para>
        /// <para>For more information, see 7.6.4.3, "File encryption key algorithm" and 7.6.4.4, 
        /// "Password algorithms".</para>
        /// </summary>
        public IPdfObject O => Get<IPdfObject>(Constants.DictionaryKeys.Encryption.Standard.O)!;

        /// <summary>
        /// (Required) A byte string, 32 bytes long if the value of R is 4 or less and 48 bytes long if 
        /// the value of R is 6, based on the owner and user password, that shall be used in determining 
        /// whether to prompt the user for a password and, if so, whether a valid user or owner password 
        /// was entered. For more information, see 7.6.4.4, "Password algorithms".
        /// </summary>
        public IPdfObject U => Get<IPdfObject>(Constants.DictionaryKeys.Encryption.Standard.U)!;

        /// <summary>
        /// (Required if R is 6 (PDF 2.0)) A 32-byte string, based on the owner and user password, that 
        /// shall be used in computing the file encryption key. For more information, see 7.6.4.4, 
        /// "Password algorithms".
        /// </summary>
        public IPdfObject? OE => Get<IPdfObject>(Constants.DictionaryKeys.Encryption.Standard.OE);

        /// <summary>
        /// (Required if R is 6 (PDF 2.0)) A 32-byte string, based on the user password, that shall be 
        /// used in computing the file encryption key. For more information, see 7.6.4.4, "Password algorithms".
        /// </summary>
        public IPdfObject? UE => Get<IPdfObject>(Constants.DictionaryKeys.Encryption.Standard.UE);

        /// <summary>
        /// (Required) A set of flags specifying which operations shall be permitted when the document is 
        /// opened with user access (see "Table 22 — Standard security handler user access permissions").
        /// </summary>
        public Integer P => Get<Integer>(Constants.DictionaryKeys.Encryption.Standard.P)!;

        /// <summary>
        /// (Required if R is 6 (PDF 2.0)) A 16-byte string, encrypted with the file encryption key, that contains an encrypted copy of the permissions flags. For more information, see 7.6.4.4, "Password algorithms".
        /// </summary>
        public IPdfObject? Perms => Get<IPdfObject>(Constants.DictionaryKeys.Encryption.Standard.Perms);

        /// <summary>
        /// (Optional; meaningful only when the value of V is 4 (PDF 1.5) or 5 (PDF 2.0)) Indicates whether 
        /// the document-level metadata stream (see 14.3.2, "Metadata streams") shall be encrypted. 
        /// Default value: true.
        /// </summary>
        public BooleanObject? EncryptMetadata => Get<BooleanObject>(Constants.DictionaryKeys.Encryption.Standard.EncryptMetadata);

        internal static StandardEncryptionDictionary FromDictionary(Dictionary dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            return new StandardEncryptionDictionary(dictionary);
        }
    }
}
