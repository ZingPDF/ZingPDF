# Support Matrix

This document records the current support expectations for ZingPDF ahead of public package distribution.

## Target framework

- `ZingPDF`: `net8.0`
- `ZingPDF.FromHTML`: `net8.0`
- `ZingPDF.Fonts`: `net8.0`
- `ZingPDF.GoogleFonts`: `net8.0`

## Operating systems

ZingPDF is developed as a managed .NET library and is expected to work on:

- Windows
- Linux
- macOS

Notes:

- Actual runtime behaviour depends on the native/runtime requirements of third-party dependencies such as `SkiaSharp` and `PuppeteerSharp`.
- `ZingPDF.FromHTML` has additional environment requirements because it depends on a browser automation stack.

## Supported scenarios

### Document core

- load an existing PDF from a seekable stream
- create a new blank PDF
- inspect page counts and retrieve pages
- append, insert, delete, and merge pages
- rotate pages
- read and update document metadata

### Page content

- add text with registered fonts
- add images including PNG support
- draw vector paths with stroke and fill options
- extract text
- add simple text watermarks

### Fonts and packages

- register standard PDF fonts and embedded TrueType fonts
- download and register Google Fonts through the `ZingPDF.GoogleFonts` package

### Forms, security, and save workflows

- work with AcroForm fields
- compress and decompress stream content
- save incremental updates
- authenticate encrypted PDFs
- encrypt a plain PDF and remove encryption from an encrypted PDF

## Current feature limits

These limits should be treated as part of the current product contract unless explicitly expanded in release notes:

- saves are incremental updates rather than full rewrites
- encryption writing supports Standard security handler RC4-128 (`V=2`, `R=3`), AES-128 (`V=4`, `R=4`), and AES-256 (`V=5`, `R=6`)
- removing encryption from an incrementally saved document does not physically remove older encrypted revisions from the file bytes
- high-level custom font registration currently targets WinAnsi / Windows-1252 text workflows
- high-level registration does not yet cover Symbol or ZapfDingbats usage
- `ZingPDF.GoogleFonts` requires a Google Fonts Developer API key and network access at registration time
- text fields currently have the richest form write support
- signature fields are discoverable but digital signing is not implemented through the high-level API
- push-button actions are not yet exposed through the high-level API
- unusual viewer-specific form appearance behaviour may still require low-level object access

## Deployment expectations

- input streams passed to `Pdf.Load(...)` must be seekable
- output streams passed to `SaveAsync(...)` must be writable and seekable
- if saving to a different stream, the target stream must be empty
- if saving back to the original stream, ZingPDF appends the update to the existing PDF

## Commercial support terms

Commercial licensing and support terms are defined in:

- `LICENSE.txt`
- `../legal/EULA.md`
- `../legal/EVALUATION_TERMS.md`
- `../legal/COMMERCIAL_TERMS.md`
- `SUPPORT_POLICY.md`
