# Support Matrix

This document records the current support expectations for ZingPDF ahead of public package distribution.

## Target framework

- `ZingPDF`: `net8.0`
- `ZingPDF.FromHTML`: `net8.0`
- `ZingPDF.GoogleFonts`: `net8.0`
- `ZingPDF.OCR`: `net8.0`
- `ZingPDF.Templates`: `net8.0`
- `ZingPDF.Templates.LiquidHtml`: `net8.0`

The package target is the minimum application framework, not a restriction to
applications running .NET 8. Applications targeting `net8.0`, `net9.0`, or
`net10.0` can reference these packages. .NET Framework 4.x and applications
targeting .NET 6 or 7 are not supported.

### Runtime validation

Windows validation on 20 October 2026 covers the unit, integration, and smoke
projects on .NET 8, 9, and 10. Each project targets all three frameworks, so the
test hosts execute on each runtime while the library continues to target `net8.0`.
The `.github/workflows/ci.yml` matrix is configured to run the same projects on
Windows, Ubuntu, and macOS with the .NET 8, 9, and 10 SDKs/runtimes installed.
Configuration of that matrix does not by itself prove a remote CI run has passed.

| Application target | Unit | Integration (real Chromium) | Smoke |
| --- | ---: | ---: | ---: |
| `net8.0` | 468 passed | 18 passed | 122 passed |
| `net9.0` | 468 passed | 18 passed | 122 passed |
| `net10.0` | 468 passed | 18 passed | 122 passed |

The .NET 9 browser run initially hit two timing failures (a four-second warmup
deadline and a stall-request wait). Both tests passed unchanged in isolation,
then the complete integration project passed unchanged. Test framework runs are
serialized rather than relaxing the existing timeout or cancellation assertions.

The separate published-package consumer check uses all six ZingPDF 1.0.59
packages from NuGet. It requires the expected runtime major version, loads each
package assembly, and checks core authoring, editing, save/reopen, two-page output,
and expected embedded text. Run it with:

```powershell
pwsh ./scripts/verify-runtime-consumers.ps1
```

Evidence and generated PDFs are retained in a unique `.tools/runtime-consumers`
directory. This consumer check does not validate every HTML or native OCR
deployment. The source integration tests require a real Chromium executable;
rendered-page OCR smoke tests exercise PDFium/SkiaSharp and an injected OCR engine.
Native Tesseract and the separate Linux CLI corpus retain their existing limits.

The downloadable homepage projects target `net8.0` by default and therefore need
the .NET 8 runtime even when built with a later SDK. Change their `TargetFramework`
to `net9.0` or `net10.0` to run on those runtimes. This does not require retargeting
the library package itself.

## HTML renderer deployment

`HtmlToPdfRenderer` accepts an installed Chromium-compatible executable and does not download by default. Reuse the caller-owned renderer for concurrent conversions; its default limit is four isolated contexts, with remaining calls queued. Options are copied at construction. Print media, background printing and CSS page sizes are the new renderer defaults. Original static converter overloads retain their browser download, screen-media and default-print behavior. Their overall conversion deadline is disabled to preserve explicit navigation timeouts; the browser startup budget remains finite.

The new renderer's default 30-second `RenderTimeout` covers queueing, startup, navigation, font readiness and printing. `Timeout.InfiniteTimeSpan` disables this overall deadline. A separate finite `BrowserStartupTimeout` covers provisioning and launch, defaulting to 30 seconds. Cancellation is forwarded through `RendererHtmlToPdfConverter`; the renderer deadline begins after Liquid rendering. Browser context creation and cleanup must finish; browser startup or downloading can remain renderer-owned after a caller stops waiting, within the separate startup budget. Disposal cancels browser provisioning. Deadline and disposal behavior depend on browser protocol responsiveness. This is not an OS-enforced hard worker bound.

Deploy browser libraries and fonts with the application and test the actual host. `Offline` is page network emulation, not a security boundary; embed assets and accept only trusted HTML/scripts. The renderer leaves the Chromium sandbox enabled unless the caller changes launch arguments. Caller disposal closes the owned browser; templates do not own the supplied renderer.

`examples/GenerateInvoices` provides a long-table invoice corpus and a Linux container recipe. Validate the recipe in the target Linux image. No Linux readiness or cross-library performance claim follows from the recipe alone.

## Operating systems

ZingPDF is developed as a managed .NET library and is expected to work on:

- Windows
- Linux
- macOS

Notes:

- Actual runtime behaviour depends on the native/runtime requirements of third-party dependencies such as `SkiaSharp` and `PuppeteerSharp`.
- Page-to-PNG rendering in `ZingPDF` uses PDFium through `PDFtoImage` and `SkiaSharp` native assets.
- `ZingPDF.FromHTML` has additional environment requirements because it depends on a browser automation stack.
- `ZingPDF.Templates.LiquidHtml` has the same browser automation requirements because it converts rendered HTML through `ZingPDF.FromHTML`.

## Supported scenarios

### Document core

- load an existing PDF from a seekable stream
- create a new blank PDF
- inspect page counts and retrieve pages
- read visible page geometry and translate points between PDF page coordinates and displayed page coordinates
- render 1-based pages to PNG bytes for previews or thumbnails
- append, insert, delete, and merge pages
- rotate pages
- read and update document metadata

### Page content

- create PDFs from Liquid HTML templates through `ZingPDF.Templates.LiquidHtml`
- add text with registered fonts
- add images including PNG support
- draw vector paths with stroke and fill options
- replace supported existing text lines through `Pdf.CreateTextEditor()`, with font coverage and overflow checks, automatic TrueType embedding, and source positions retained
- extract text, including inline marked-content property dictionaries and positioned line boundaries across a page's content streams
- OCR image-based pages through the `ZingPDF.OCR` package
- add simple text watermarks

### Fonts and packages

- register standard PDF fonts and embedded TrueType fonts; `RegisterUnicodeTrueTypeFontAsync` creates Unicode CID mappings automatically, with font coverage checks and measured advances
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
- `RegisterTrueTypeFontAsync` retains WinAnsi / Windows-1252 encoding; use `RegisterUnicodeTrueTypeFontAsync` for Unicode TrueType authoring. Unicode registration embeds the full font, checks glyph coverage, and does not provide shaping or fallback.
- high-level registration does not yet cover Symbol or ZapfDingbats usage
- `ZingPDF.GoogleFonts` can download TrueType variants without a key; family metadata and variable-font preference require the keyed Developer API path. The keyless CSS path requests Latin, extended Latin, Greek, Cyrillic and Vietnamese scripts; check glyph coverage before use. Initial downloads require network access.
- `ZingPDF.Templates.LiquidHtml` uses HTML/CSS browser rendering and is not a PDF-native layout engine
- `ZingPDF.OCR` defaults to embedded text or one supported image XObject; opt into `PdfOcrMode.RenderedPage` for OCR of the visible page through PDFium, including mixed content and scan tiles. Result provenance describes input coverage rather than recognition completeness.
- `Page.RenderAsync(...)` and `Pdf.RenderPageAsync(...)` render PNG bytes through a native PDFium-backed dependency and should be validated on each deployment platform
- text fields currently have the richest form write support
- form flattening requires usable normal widget appearances; missing or malformed appearances reject before widget removal, and XFA flattening is unsupported
- signing currently requires unencrypted input and unencrypted output; password-protected input signing is not implemented yet
- encrypting an already signed PDF is not a supported high-level workflow because rewriting signed bytes invalidates existing signatures
- signature validation does not yet validate trusted timestamp tokens, DSS/VRI long-term validation data, or DocMDP certification permissions
- push-button action dictionaries are exposed as metadata; form reset is not implemented yet
- unusual viewer-specific form appearance behaviour may still require low-level object access

### Existing-line editing limits

`Pdf.CreateTextEditor()` reads and replaces same-style text on one baseline in
page and Form XObject content streams. Compatible runs can span adjacent page content streams and equivalent font aliases; effective painting state is compared rather than command history. Simple fonts need a reliable Unicode
mapping; composite fonts currently require Identity-H. It preserves surrounding
content and clones shared streams for the edited occurrence. Replacements can
supply a registered `PdfFont` or TrueType bytes; embedding, CID widths, CIDToGIDMap and ToUnicode are
created automatically. A full embedded TrueType program can be remapped when it
contains missing characters; an actual subset cannot recover absent glyphs.

Paragraph reflow, mixed-style paragraph editing, vertical writing, complex-script
shaping, Type 3 fonts, invisible/clipping text modes, ExtGState font selection and
marked content with ActualText are unsupported for replacement. These runs do not disable supported text elsewhere on the page. Rejected checks leave page content
unchanged. The operation follows normal incremental-save semantics and is not secure
redaction. Keep input streams open, readable and seekable; outputs must be empty,
writable and seekable. See [text editing](TEXT_EDITING.md).

## Desktop editor MVP

`apps/ZingPDF.Editor` is an Avalonia desktop application targeting .NET 10 and
referencing the repository's ZingPDF library. It adds text, rotates/reorders/deletes
pages, appends PDFs without forms, exports selected pages, fills supported text
fields, replaces supported existing text lines, and saves a copy. Missing characters
and overflow are checked before applying. Users can choose a Google font, which is
embedded automatically. The PDF is never uploaded. Image insertion and OCR
editing are not implemented. Added text and field writes use Windows-1252
characters; existing-line replacement can use further left-to-right Unicode characters
covered by its selected font. Encrypted and digitally signed input is rejected.

The Pages tab supports single-page drag reordering, including across wrapped
thumbnail rows, an insertion indicator, and edge scrolling. Escape or releasing
outside the overview cancels a drag. Completed moves support undo and redo;
the move buttons remain available for keyboard use, with Move to for arbitrary destinations.

The editor renders visible thumbnails on demand, retaining at most 64 images and
creating at most 48 overview cards at a time. The sidebar is virtualized. Go to
jumps directly to a page. Input PDFs above 256 MiB are rejected before allocation;
document revisions still reside in memory. The local Recent list retains up to
ten opened/saved file paths, can be cleared without deleting files, and does not
archive earlier document versions.

The editor opens at Fit page by default and offers Fit width and fixed zoom
percentages. Fit modes follow the window size and the page's displayed crop and
rotation. The chosen zoom mode is stored locally between launches; changing zoom
does not apply drafts or change the PDF.

The initial publish target is Windows x64. Other desktop platforms require
runtime and packaging validation. No standalone editor price, bundle entitlement,
or activation policy is introduced by this developer MVP. Build instructions,
verification commands, and product limits are in
[`apps/ZingPDF.Editor/README.md`](../../apps/ZingPDF.Editor/README.md).

## Deployment expectations

- repository builds use ImageSharp 3.2.0 and require a private SixLabors build licence; see [build-licence setup](WORKFLOWS.md#sixlabors-build-licence)
- applications consuming ZingPDF with ImageSharp only as a transitive dependency are not directly subject to ImageSharp's build-time key validation; directly referencing ImageSharp has its own licence requirements

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
- versions released during a paid subscription remain licensed for commercial use within the purchased scope after cancellation or expiry; new versions and support require an active subscription
- the core library targets `net8.0`, with .NET 8 as the minimum; Windows runtime validation covers .NET 8, 9, and 10, while Windows, Linux, and macOS deployments retain their native dependency requirements
- the core library is suitable for desktop apps, services, workers, background jobs, and CLI tools

## Commercial support terms

Commercial licensing and support terms are defined in:

- `LICENSE.txt`
- `../legal/EULA.md`
- `../legal/EVALUATION_TERMS.md`
- `../legal/COMMERCIAL_TERMS.md`
- `SUPPORT_POLICY.md`

## Editor-facing API behaviour

- `MovePageAsync(from, to)` uses final 1-based page indices, preserves page object identities, inherited geometry/resources and widgets. It does not remap page-label ranges or numeric page references in application data.
- `Page.AddTextAsync(text, baselineOrigin, font, size, colour, placement)` handles explicit line breaks and counterclockwise text rotation. Existing graphics state is isolated before high-level page additions.
- Font resource-name collisions are assigned page-local aliases; shared resource dictionaries and original text are preserved. A font registered in another document is rejected.
- Font program stream properties resolve compatible generic stream dictionaries for both created and loaded streams, retaining decryption behaviour. Stream parsers consume only the stream delimiter line ending, preserving leading binary whitespace, including AES initialization-vector bytes.
- `IsEncryptedAsync()` includes queued encryption/removal settings. It does not authenticate or grant permissions.
