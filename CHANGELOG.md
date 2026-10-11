# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project aims to follow Semantic Versioning.

## [Unreleased]

## [1.0.59] - 2026-10-11

### Added

- PDF page-to-PNG rendering through PDFium, with visible-box and page-rotation options (#12).
- A reusable `HtmlToPdfRenderer` for caller-provisioned Chromium, isolated contexts, bounded concurrency, print settings, cancellation and startup controls; a Liquid HTML adapter uses the same renderer (#13).
- Opt-in `PdfOcrMode.RenderedPage` with DPI/pixel limits and source/input-coverage metadata for mixed pages and scan tiles (#14).
- Runnable offline Linux invoice and native OCR acceptance examples, independent form appearance comparisons, and procurement evaluation reports.

### Fixed

- Inherited and repeated external form widget appearances, checkbox/radio selections and choice export/display values. Flattening preflights usable normal appearances and rejects unsupported XFA before destructive changes (#14).
- Positioned plain-text line breaks and gaps across a page's content streams, while preserving contiguous same-line fragments (#14).
- Default parser logging no longer creates an unused debug file, allowing read-only application directories (#14).
- Text extraction from inline marked-content property dictionaries and a signing certificate fixture timing race (#13).

### Changed

- Clarify continuing rights to paid versions in the licence documentation and add website sales-funnel measurement and Google Analytics configuration.
- Original static HTML converter overloads retain their existing download and rendering defaults; full-page OCR requires an explicit mode selection.

## [1.0.58] - 2026-05-28

### Changed

- Add page geometry APIs (#8)

## [1.0.57] - 2026-05-25

### Added

- Standard security handler AES-128 (`V=4`, `R=4`) and AES-256 (`V=5`, `R=6`) encryption support alongside legacy RC4 output
- Push-button caption and action inspection through `PushButtonFormField`, including action type, URI/named actions, and additional-action triggers
- Signature field inspection through `SignatureFormField`, including signed-state, filter, subfilter, signer name, reason, and signing date
- Public field-bounds inspection through `IFormField.GetFieldBoundsAsync()`

## [1.0.0] - 2026-03-25

### Added

- Blank PDF creation through `Pdf.Create(...)`
- Metadata inspection and editing through `GetMetadataAsync()`
- Watermarking support
- Compression support for eligible streams
- Encryption and decryption APIs for password-protected PDFs
- Public button-field support in the forms API
- XML documentation for the primary public API
- NuGet packaging metadata, packaged README, packaged license notice, and symbol package generation
- Commercialization checklist for release planning
- Commercial license notice, EULA, evaluation terms, commercial terms, and support policy
- Support matrix for shipping packages
- Package-specific readmes for `ZingPDF.Fonts` and `ZingPDF.FromHTML`
- Maintainer-focused repository README and website-based developer documentation
- Release-ready package metadata for `ZingPDF`, `ZingPDF.Fonts`, and `ZingPDF.FromHTML`

### Changed

- `InsertPageAsync(...)` now inserts at the requested location instead of appending
- `AppendPdfAsync(...)` now maintains page counts correctly when merging page trees
- Save behavior now updates document metadata to credit `ZingPDF` as producer
- Save-time metadata handling now tolerates existing info dates stored as either PDF date objects or strings
- Encryption APIs now use real-world password-based signatures
- Core package nullable warning backlog in shipping paths has been resolved
- Release/readme/site copy now reflects the commercial subscription model and bundled package set

### Fixed

- Form updates are awaited before save output is generated
- Save validation now rejects invalid output streams more clearly
- Public forms enumeration is more resilient when optional field descriptions are missing
