using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Signature
{
    /// <summary>
    /// Represents a signature field.
    /// </summary>
    /// <remarks>
    /// This type currently supports signature metadata inspection. Digital signing is not yet implemented through this API.
    /// </remarks>
    public class SignatureFormField : FormField<IPdfObject>
    {
        /// <summary>
        /// Initializes a signature field wrapper.
        /// </summary>
        public SignatureFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            FieldProperties properties,
            Form parent,
            IPdf pdf
            )
            : base(fieldIndirectObject, name, description, properties, parent, pdf)
        {
        }

        /// <summary>
        /// Gets whether the field currently contains a signature dictionary value.
        /// </summary>
        public async Task<bool> HasSignatureValueAsync()
            => await GetSignatureDictionaryAsync() is not null;

        /// <summary>
        /// Gets the signature handler filter name when present.
        /// </summary>
        public async Task<string?> GetFilterAsync()
            => (await GetSignatureDictionaryAsync())?.GetAs<Name>("Filter")?.Value;

        /// <summary>
        /// Gets the signature subfilter name when present.
        /// </summary>
        public async Task<string?> GetSubFilterAsync()
            => (await GetSignatureDictionaryAsync())?.GetAs<Name>("SubFilter")?.Value;

        /// <summary>
        /// Gets the signer name when present.
        /// </summary>
        public async Task<string?> GetSignerNameAsync()
            => (await GetSignatureDictionaryAsync())?.GetAs<PdfString>("Name")?.DecodeText();

        /// <summary>
        /// Gets the human-readable signing reason when present.
        /// </summary>
        public async Task<string?> GetReasonAsync()
            => (await GetSignatureDictionaryAsync())?.GetAs<PdfString>("Reason")?.DecodeText();

        /// <summary>
        /// Gets the signature timestamp when present.
        /// </summary>
        public async Task<DateTimeOffset?> GetSigningDateAsync()
            => (await GetSignatureDictionaryAsync())?.GetAs<Date>("M")?.DateTimeOffset;

        private async Task<Dictionary?> GetSignatureDictionaryAsync()
            => await _fieldDictionary.V.GetAsync() as Dictionary;
    }
}
