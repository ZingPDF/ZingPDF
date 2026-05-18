using System.Security.Cryptography.X509Certificates;
using ZingPDF.Extensions;
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
            => (await GetSignatureDictionaryAsync())?.GetAs<PdfString>("Name")?.Decode();

        /// <summary>
        /// Gets the human-readable signing reason when present.
        /// </summary>
        public async Task<string?> GetReasonAsync()
            => (await GetSignatureDictionaryAsync())?.GetAs<PdfString>("Reason")?.Decode();

        /// <summary>
        /// Gets the signature timestamp when present.
        /// </summary>
        public async Task<DateTimeOffset?> GetSigningDateAsync()
            => (await GetSignatureDictionaryAsync())?.GetAs<Date>("M")?.DateTimeOffset;

        /// <summary>
        /// Signs this field with a detached CMS payload using the supplied certificate.
        /// </summary>
        public async Task SignAsync(X509Certificate2 certificate, PdfSignatureOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            if (_pdf is not Pdf pdf)
            {
                throw new NotSupportedException("High-level signing requires the default Pdf implementation.");
            }

            await pdf.QueueSignatureAsync(_fieldIndirectObject, certificate, options ?? new PdfSignatureOptions());
        }

        /// <summary>
        /// Validates the signature dictionary associated with this field.
        /// </summary>
        public async Task<PdfSignatureValidationResult> ValidateSignatureAsync(PdfSignatureValidationOptions? options = null)
        {
            if (_pdf is not Pdf pdf)
            {
                throw new NotSupportedException("Signature validation requires the default Pdf implementation.");
            }

            var signature = await PdfSignature.CreateAsync(pdf, this)
                ?? throw new InvalidOperationException("The signature field does not contain a signature dictionary value.");

            return await signature.ValidateAsync(options);
        }

        internal async Task<Dictionary?> GetSignatureDictionaryForValidationAsync()
            => await GetSignatureDictionaryAsync();

        private async Task<Dictionary?> GetSignatureDictionaryAsync()
            => await _fieldDictionary.V.GetAsync() as Dictionary;
    }
}
