# Existing text editing

`Pdf.CreateTextEditor()` exposes existing-line analysis and replacement. The desktop
editor uses the same API; it does not cover old text with a white rectangle.

## Current workflow

1. `GetLinesAsync(pageNumber)` reads same-style fragments on one baseline, including
   Form XObject occurrences. It returns Unicode text, font information, page-space
   bounds and a revision-specific identifier. Page numbers are 1-based.
2. `CheckReplacementAsync(line, text, options)` checks encoding coverage and available
   baseline width. Unsupported content and missing characters are explicit results.
3. `ReplaceLineAsync(...)` rewrites the selected text-showing operators, preserving
   subsequent text matrices, painting state and unrelated content. Shared streams and
   nested Forms are copied for the selected occurrence. Read fresh lines after edits.
4. Save through the usual PDF save API. Inputs must remain readable and seekable;
   outputs must be empty, writable and seekable.

Supply a registered `PdfTextReplacementOptions.Font`, or `TrueTypeFontData` when choosing another font.
Registered fonts are shared with normal authoring APIs and must belong to the same document.
Embedding permissions and glyph coverage are checked. The library creates a Type0
Identity-H wrapper, explicit CIDToGIDMap, widths and ToUnicode mapping. A complete
embedded TrueType program can extend its mapping when it already contains the needed
glyphs. A subset cannot recover absent glyphs: choose another font. Replacement
fonts are embedded automatically; the UI requires no embedding configuration.

The desktop editor offers Google font family/style selection and a local font cache.
Its keyless requests contain family, variant and fixed script ranges, never PDF text.
Google responses are checked locally for TrueType embedding and character coverage.
An unavailable family or insufficient coverage leaves the choice dialog open. Longer
lines require shortening, reducing size or explicitly allowing overflow. Pending
changes can be discarded; applied edits use the existing undo/redo history.

## Boundaries

- One same-style baseline, without paragraph reflow or mixed-style paragraph editing.
- Simple fonts with reliable mappings, and horizontal Identity-H composite fonts. Standard Type 1 Latin fonts without an Encoding entry use Adobe StandardEncoding, including its distinct quotation and ligature codes.
- No Type 3 fonts, private unmapped embedded encodings, vertical writing, complex-script
  shaping, invisible/clipping text modes, ExtGState font selection or ActualText replacement.
  Unsupported ExtGState/ActualText text is reported per line; other text remains editable.
- Bounds use font ascent/descent and text advances, rather than precise glyph outlines.
- The width check uses the page edge and later text on the baseline. It does not detect
  arbitrary collisions with drawings, images, columns or content on other baselines.
- Incremental saving can retain old text in previous revisions. This is not secure redaction.
- The editor still rejects signed and encrypted input; this work does not expand that scope.

## Grouping

An editable line contains compatible same-style runs on one baseline. It can span
adjacent page content streams and equivalent font resource aliases. Repeating the
same colour-setting operator does not split a line. Changing font, size, spacing,
clipping, marked-content scope or painting state can still split it. Independent
Form occurrences remain separate editing units. These are editable groups, not
semantic paragraphs or every possible visual line. Handles cover every contributing
source stream; replacements preserve subsequent text positioning across stream boundaries.

## Authoring without PDF object manipulation

```csharp
using ZingPDF;
using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics;
using ZingPDF.Text;
using ZingPDF.Text.Editing;

using var pdf = Pdf.Create();
var font = await pdf.RegisterUnicodeTrueTypeFontAsync("NotoSans-Regular.ttf");
var page = await pdf.GetPageAsync(1);
await page.AddTextAsync("First line\nSecond line", new Coordinate(40, 700), font, 14,
    RGBColour.Black, new TextPlacementOptions { RotationDegrees = 0, LineHeight = 18 });

var editor = pdf.CreateTextEditor();
var line = (await editor.GetLinesAsync(1)).First();
var options = new PdfTextReplacementOptions { Font = font };
var check = await editor.CheckReplacementAsync(line, "Revised wording", options);
if (!check.CanApply) throw new InvalidOperationException(check.Message);
await editor.ReplaceLineAsync(line, "Revised wording", options);
using var output = File.Create("edited.pdf");
await pdf.SaveAsync(output);
```

`PdfFont.GetMissingCharacters(text)` checks representability before authoring;
Unicode fonts check actual glyph coverage. `MeasureText(text, size)` measures a
single line's advance without kerning or shaping. The Unicode character mappings
expand automatically as further text is added. Complete font programs are embedded;
font subsetting is not implemented. Registration copies font data and does not close
a caller-provided font stream. Fonts belong to the document that registered them.

`RegisterTrueTypeFontAsync` retains its WinAnsi behaviour for compatibility.
Use `RegisterUnicodeTrueTypeFontAsync` for broader character coverage, or fluent
`WithUnicodeTrueTypeFont`. Google Fonts has corresponding
`RegisterUnicodeGoogleFontAsync` and `WithUnicodeGoogleFont` helpers.

Page text/image/path additions isolate existing graphics state automatically.
Text added at a baseline supports CRLF/CR/LF and counterclockwise rotation in page
coordinates. Call `pdf.MovePageAsync(from, to)` to reorder 1-based page indices;
page objects, inherited geometry/resources and widgets are retained. Page-label
ranges and numeric page indices in application metadata are not remapped.
`pdf.IsEncryptedAsync()` reads encryption state without trailer access and includes
queued encryption/removal settings; it does not authenticate or grant permissions.

## Work needed for paragraph editing

1. Reconstruct words, style runs, reading order and paragraph boundaries from positioned
   content, with explicit ambiguity handling for columns, tables, headings and rotated text.
2. Maintain an editable layout model separate from the original content operators. Define
   paragraph bounds, wrapping, line height, alignment, overflow and neighbouring-object rules.
3. Add mixed font/style spans, font fallback, glyph shaping, bidirectional layout, ligatures
   and script-specific line breaking. Font coverage alone does not provide shaping.
4. Provide caret navigation, selection across runs/lines, keyboard and input-method support,
   plus visible wrap/overflow previews and a way to resize the text area.
5. Validate round trips across producer-generated PDFs, embedded subsets, custom encodings,
   transforms, transparency, clipping, tagged content and shared resources. Compare rendering,
   extraction and object preservation after save/reopen and undo/redo.

Scanned text requires a separate OCR and image-replacement workflow. It does not become
editable by reconstructing text operators, because the original page may contain only pixels.
