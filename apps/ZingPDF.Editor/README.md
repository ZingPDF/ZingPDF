# ZingPDF Editor MVP

A local desktop PDF editor built with Avalonia, CommunityToolkit.Mvvm, and the repository's ZingPDF library. The application targets .NET 10; the library continues to target .NET 8. This is a developer MVP, not a published commercial release.

## Run

From the repository root, with the .NET 10 SDK installed and the private SixLabors
build licence configured as described in [repository workflows](../../docs/project/WORKFLOWS.md#sixlabors-build-licence):

```powershell
pwsh ./scripts/run-editor.ps1
pwsh ./scripts/run-editor.ps1 -Demo
pwsh ./scripts/run-editor.ps1 -PdfPath 'C:\Documents\input.pdf'
```

The welcome screen also offers a sample PDF with three pages and two text fields. It is created under the current user's local application data directory. Existing sample files are retained.

## Supported operations

- Open one local PDF at a time, up to 256 MiB. Files are read into memory; PDF content stays on this computer. Choosing a Google font can download font data; no account or API key is needed.
- The header uses the existing ZingPDF brand mark. Text, Pages, and Forms tabs use icons and labels. Select and Add text form a mutually exclusive tool selector; **V** selects and **T** places text when the focus is outside a text input. The pointer changes to an I-beam for placement, an I-beam over editable existing lines, a move cursor over added text, and a hand over draggable page cards.
- Add black Helvetica text, including explicit line breaks. Select added text to change its wording and size, then choose **Apply changes**. Click a supported existing line to replace its wording or change its size. The original position and colour are retained. Missing characters and line overflow are checked before applying. Choose a Google font when the original font lacks coverage; embedding is automatic. Discard changes abandons a pending line draft.
- Drag an added text outline to move it. With the page focused, arrow keys move selected text by one PDF display unit; Shift moves by ten. Delete removes the selected addition; Escape leaves placement mode. In placement mode, Enter places text in the centre for keyboard use.
- Open with **Fit page** by default. Choose **Fit width** to read across the available width, or a fixed percentage from 50% to 200%. Automatic modes recalculate when the window, page, or page orientation changes; their calculated zoom is bounded to 1–400%. Placement uses page geometry, including crop offsets and rotations. The preview is rendered from the same PDF content used to save.
- Rotate, move, delete, and export pages. In **Pages**, drag a thumbnail before or after another page; the blue insertion line marks the drop position. Hold near the top or bottom edge to scroll. Escape or dropping outside the overview cancels the drag. Each completed move is one undo step, and the moved page stays selected. The arrow buttons move a page one position; **Move to…** chooses any destination, including another overview group. Add pages from another PDF that has no AcroForm. Keep at least one page.
- Fill existing, writable, non-password AcroForm text fields using the field panel. Field drafts survive switching fields and are applied before saving or changing tasks.
- Undo and redo document edits. History retains up to 30 revisions and prunes older snapshots when retained PDF byte storage exceeds 128 MiB. A single large revision can exceed that limit.
- Save a copy or export the selected page. The original path is rejected as a destination. Output is staged beside the destination before replacement; cancelled dialogs and failed saves do not overwrite the original.
- Closing or opening another document with unsaved edits offers Save a copy, Discard changes, or Cancel.

The fixed top bar owns file operations and history. Task groups reveal relevant tools. The document stays central; text and form settings appear only for the relevant selection or task. Only visible thumbnail cards are rendered, in batches of up to eight background requests. The page sidebar uses a virtualized list; the Pages overview shows at most 48 cards at a time, with Previous/Next controls. **Go to…** jumps directly to a page. The thumbnail cache retains at most 64 images; old previews stay visible while replacements load after edits. Selecting an overview card does not render the full-size page until returning to Text or Forms. During edits the workspace keeps its previous preview visible while rendering the replacement; pointer and keyboard edits are blocked until the operation completes. Page operations update thumbnail containers in place, and the text properties panel keeps a fixed width to avoid moving the page when selecting text.

The zoom selector is in the bottom-right corner of the page workspace. The chosen mode or fixed percentage is remembered between documents and launches in `preferences.json` under the user's local application data `ZingPDF Editor` directory. It also stores up to ten recent file paths, but no PDF content. **Recent** is available in the header and on the welcome screen; it includes successfully opened PDFs and saved copies. Clearing the list does not delete files or reset the zoom preference. Recent paths are kept only on this computer. This list is separate from edit undo/redo; neither document contents nor earlier saved versions are archived. Missing, invalid, or unreadable preferences fall back to **Fit page**. Zooming and resizing do not apply pending text or form drafts, modify the PDF, or add undo history. Page-overview thumbnails use a fixed size independently of page zoom.

## Current limits

- Windows x64 is the initial packaged target. Avalonia supports other desktop platforms, but macOS/Linux packaging and native PDF rendering need validation on those operating systems.
- Added text and form writes are limited to Western European / Windows-1252 characters. Unsupported characters are rejected before updating the document. Existing-line replacements with a selected TrueType font can use additional left-to-right Unicode characters when that font contains them.
- Signed and encrypted PDFs are rejected. The editor does not silently invalidate a digital signature or remove password protection.
- Existing-line replacement supports reliable Unicode mappings, simple fonts and horizontal Identity-H fonts, including Form XObject occurrences. Matching runs can span adjacent page content streams and equivalent font aliases; repeated colour commands do not force a split. Unsupported ActualText/graphics-state font runs leave other text editable. It rewrites text-showing operators; it is not secure redaction. Font subsets retain only their available characters unless a replacement font is selected. Complete embedded TrueType programs can extend their Unicode mapping automatically when the glyphs exist.
- No automatic paragraph reflow, mixed-style paragraph editing, vertical writing, complex-script shaping, text clipping, ExtGState font selection, marked-content ActualText replacement, image insertion, OCR editing, annotations, redaction, signature creation, or form creation UI.
- Adding a PDF with an AcroForm is rejected because complete field merging is outside this MVP. Exported pages use ZingPDF's page-copy behavior; preservation of interactive form behavior in exports is not promised.
- Added text remains separately editable during the current session. Reopening a saved copy treats it as ordinary page content.
- Undo history and drafts are in memory; there is no crash recovery or autosave.
- Raster previews are capped at 5,000 pixels per dimension. Rendered PNG results are cached per revision with a four-entry / 16 MiB budget. The editor still holds document bytes and revisions in memory. Inputs above 256 MiB are rejected before allocating the document buffer; appending rejects source-size totals above that limit. The 240-page synthetic test proves lazy previews, bounded thumbnail/card counts, navigation, and saved reorder behavior; it is not a benchmark of large scanned files.
- No installer, code signing, update service, paid licence activation, or standalone commercial terms are supplied by this MVP. Repository licensing still applies.

Downloaded font files are cached under `ZingPDF Editor/Fonts` in local application data. Requests identify the family, style and fixed script ranges, never PDF text. The keyless Google Fonts path requests Latin, extended Latin, Greek, Cyrillic and Vietnamese coverage. Coverage is checked locally; some families, variants and scripts are unavailable. Cached fonts work offline.

## Build and verify

```powershell
dotnet build apps/ZingPDF.Editor/ZingPDF.Editor.csproj --configuration Release
dotnet test tests/ZingPDF.Editor.Tests/ZingPDF.Editor.Tests.csproj --configuration Release
pwsh ./scripts/publish-editor.ps1
```

The publish script creates a self-contained Windows x64 folder under `artifacts/editor/win-x64`; run `ZingPDF.Editor.exe` there without installing .NET. All files in that folder are required.

Tests create PDFs at runtime and exercise saved-file text, source preservation, history, forms, page operations, and exact preview/saved raster equality on cropped pages at 0/90/180/270 degrees. Avalonia headless tests load the real window and exercise placement, typing, dragging, zoom, task switching, field drafts, failed opens, save/reopen, and refresh stability (an undimmed workspace, retained thumbnail containers, and a fixed page position when selecting text). Set `ZINGPDF_EDITOR_EVIDENCE_DIR` to an absolute directory to retain rendered window screenshots.

The library references ImageSharp 3.2.0, which includes security fixes for the
previous 3.1.11 dependency. Building the repository requires a valid SixLabors
licence. Keep licence files and keys outside source control. The editor also pins the PKCS security update in its
application project. It does not expose image insertion or processing.

The editor now uses `MovePageAsync`, baseline `Page.AddTextAsync` with `TextPlacementOptions`,
and `IsEncryptedAsync` directly. Page-tree changes, original-content isolation, rotated text
operators and font resource aliases are handled by the library.
