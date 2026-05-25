# Signing And Encryption Plan

This note records the product boundary and implementation plan for signing and encryption interactions.

## Product Boundary

- Generic "encrypt this already signed PDF" is not a high-level workflow. Rewriting a signed file changes signed bytes and invalidates existing signatures.
- High-level encryption should target unsigned PDFs.
- High-level signing currently targets unencrypted input and unencrypted output.
- A useful future workflow is signing password-protected input: authenticate the encrypted PDF, add a signature, and save signed plain output. Encrypted signed output should only exist as a future explicit combined workflow if the library owns the final serialization order.

## Required Guardrails

1. Detect existing signatures before encryption writes.
2. Reject high-level encryption of signed documents unless a future explicit combined signed-encrypted workflow owns the final serialization order.
3. Keep save errors precise:
   - encrypted input signing is not implemented
   - encrypted signed output is not implemented
   - encrypting an already signed PDF is not supported
4. Document any future combined signing/encryption API as signing the final serialized bytes, not as a post-signing encryption rewrite.

## Implementation Phases

### Phase 1: Guard Existing Signatures During Encryption

- Add a helper that checks `Pdf.GetSignaturesAsync()` or signature fields for existing `/V` signature dictionaries before applying encryption writes.
- Make `EncryptAsync(...)` or `SaveAsync(...)` reject encryption when signed fields are present.
- Add smoke coverage proving a signed PDF cannot be encrypted through the high-level API.

### Phase 2: Sign Password-Protected Input To Plain Output

- Allow signing after `AuthenticateAsync(...)` when the output is plain and the save path rewrites final decrypted bytes.
- Keep encrypted output blocked in this phase.
- Add smoke coverage:
  - encrypted input, authenticate, sign visible field, save plain output, validate signature
  - encrypted input, wrong password, signing path fails before saving
  - encrypted input, authenticate, hidden signature, save plain output, validate signature

### Phase 3: Explicit Signed Encrypted Output, If Needed

- Design a deliberate API or save option for creating signed encrypted output in one operation.
- Serialize the final encrypted representation with reserved `/ByteRange` and `/Contents` placeholders.
- Compute CMS over the exact final byte ranges.
- Patch the reserved signature contents without changing the signed byte ranges.
- Add smoke coverage that reopens with the password and validates the signature.

## Non-Goals

- Do not silently encrypt previously signed documents.
- Do not preserve existing signatures after a full encryption rewrite.
- Do not imply that appending encrypted incremental updates after signing is the normal high-level behavior.
