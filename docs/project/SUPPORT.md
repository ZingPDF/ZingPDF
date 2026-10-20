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
- extract text, including inline marked-content property dictionaries and positioned line boundaries across a page's content streams
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
- `ZingPDF.OCR` defaults to embedded text or one supported image XObject; opt into `PdfOcrMode.RenderedPage` for OCR of the visible page through PDFium, including mixed content and scan tiles. Result provenance describes input coverage rather than recognition completeness.
- `Page.RenderAsync(...)` and `Pdf.RenderPageAsync(...)` render PNG bytes through a native PDFium-backed dependency and should be validated on each deployment platform
- text fields currently have the richest form write support
- form flattening requires usable normal widget appearances; missing or malformed appearances reject before widget removal, and XFA flattening is unsupported
- signing currently requires unencrypted input and unencrypted output; password-protected input signing is not implemented yet
- encrypting an already signed PDF is not a supported high-level workflow because rewriting signed bytes invalidates existing signatures
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
