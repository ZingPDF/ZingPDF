# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project aims to follow Semantic Versioning.

## [Unreleased]

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

### Changed

- `InsertPageAsync(...)` now inserts at the requested location instead of appending
- `AppendPdfAsync(...)` now maintains page counts correctly when merging page trees
- Save behavior now updates document metadata to credit `ZingPDF` as producer
- Save-time metadata handling now tolerates existing info dates stored as either PDF date objects or strings
- Encryption APIs now use real-world password-based signatures

### Fixed

- Form updates are awaited before save output is generated
- Save validation now rejects invalid output streams more clearly
- Public forms enumeration is more resilient when optional field descriptions are missing

## [0.1.0] - 2026-03-24

### Added

- Initial packaged commercial preview of ZingPDF for .NET 8
- Core document loading, saving, page editing, text extraction, forms, encryption, metadata, compression, and watermark APIs
