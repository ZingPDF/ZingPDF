# Support Matrix

This document records the current support expectations for ZingPDF ahead of public package distribution.

## Target framework

- `ZingPDF`: `net8.0`
- `ZingPDF.FromHTML`: `net8.0`
- `ZingPDF.GoogleFonts`: `net8.0`
- `ZingPDF.OCR`: `net8.0`
- `ZingPDF.Templates`: `net8.0`
- `ZingPDF.Templates.LiquidHtml`: `net8.0`

## Operating systems

ZingPDF is developed as a managed .NET library and is expected to work on:

- Windows
- Linux
- macOS

Notes:

- Actual runtime behaviour depends on the native/runtime requirements of third-party dependencies such as `SkiaSharp` and `PuppeteerSharp`.
- `ZingPDF.FromHTML` has additional environment requirements because it depends on a browser automation stack.
- `ZingPDF.Templates.LiquidHtml` has the same browser automation requirements because it converts rendered HTML through `ZingPDF.FromHTML`.

## Supported scenarios

### Document core

- load an existing PDF from a seekable stream
- create a new blank PDF
- inspect page counts and retrieve pages
- append, insert, delete, and merge pages
- rotate pages
- read and update document metadata

### Page content

- create PDFs from Liquid HTML templates through `ZingPDF.Templates.LiquidHtml`
- add text with registered fonts
- add images including PNG support
- draw vector paths with stroke and fill options
- extract text
- OCR image-based pages through the `ZingPDF.OCR` package
- add simple text watermarks

### Fonts and packages

- register standard PDF fonts and embedded TrueType fonts
- download and register Google Fonts through the `ZingPDF.GoogleFonts` package

### Forms, security, and save workflows

- work with AcroForm fields
- fill and flatten forms
- sign existing visible signature fields
- add hidden validation-only signature fields
- validate PDF signature byte ranges and detached CMS payloads
- optionally build the signer certificate chain with extra certificates or custom trusted roots
- compress and decompress stream content
- save incremental updates
- authenticate encrypted PDFs
- encrypt a plain PDF and remove encryption from an encrypted PDF

## Current feature limits

These limits should be treated as part of the current product contract unless explicitly expanded in release notes:

- `SaveAsync(...)` writes an incremental update by default
- `RemoveHistoryAsync()` switches saving to a rewritten-file path that removes earlier revisions from the output
- encryption writing supports Standard security handler RC4-128 (`V=2`, `R=3`), AES-128 (`V=4`, `R=4`), and AES-256 (`V=5`, `R=6`)
- removing encryption from an incrementally saved document does not physically remove older encrypted revisions from the file bytes
- high-level custom font registration currently targets WinAnsi / Windows-1252 text workflows
- high-level registration does not yet cover Symbol or ZapfDingbats usage
- `ZingPDF.GoogleFonts` requires a Google Fonts Developer API key and network access at registration time
- `ZingPDF.Templates.LiquidHtml` uses HTML/CSS browser rendering and is not a PDF-native layout engine
- `ZingPDF.OCR` works best on image-based pages and supported image XObjects rather than arbitrary rendered page content
- text fields currently have the richest form write support
- signing encrypted PDFs is outside the current high-level signing API; visible signing uses an existing or separately created signature field
- signature validation does not yet validate trusted timestamp tokens, DSS/VRI long-term validation data, or DocMDP certification permissions
- push-button action dictionaries are exposed as metadata; form reset is not implemented yet
- unusual viewer-specific form appearance behaviour may still require low-level object access

## Deployment expectations

- input streams passed to `Pdf.Load(...)` must be seekable
- output streams passed to `SaveAsync(...)` must be writable and seekable
- if saving to a different stream, the target stream must be empty
- if saving back to the original stream, ZingPDF appends the update to the existing PDF

## Technical evaluation notes

- ZingPDF keeps the input stream open and resolves objects lazily rather than materializing the whole document up front
- parsed objects are cached after resolution, and compressed object streams use a bounded cache
- some save paths still stage full output in memory, including signing and rewriting back to the source stream
- `Pdf` instances should be treated as not thread-safe and should not be shared across concurrent operations
- malformed content streams are tolerated more than damaged file structure; the loader can recover from some missing or invalid `startxref` cases when a classic xref table still exists near the end of the file

## Evaluation FAQ

- install `ZingPDF` by default; add `ZingPDF.FromHTML`, `ZingPDF.Templates.LiquidHtml`, `ZingPDF.GoogleFonts`, or `ZingPDF.OCR` only when you need those specific features
- evaluation and other non-commercial use are free; paid seats are required for commercial use and internal business operations outside genuine evaluation
- seats are licensed per developer, and contractors may use seats licensed to the same customer legal entity
- the core library targets `net8.0` and is intended for Windows, Linux, and macOS environments that support .NET 8
- the core library is suitable for desktop apps, services, workers, background jobs, and CLI tools

## Commercial support terms

Commercial licensing and support terms are defined in:

- `LICENSE.txt`
- `../legal/EULA.md`
- `../legal/EVALUATION_TERMS.md`
- `../legal/COMMERCIAL_TERMS.md`
- `SUPPORT_POLICY.md`
