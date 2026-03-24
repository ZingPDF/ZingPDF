# ZingPDF

ZingPDF is a .NET PDF library for reading, editing, and saving PDF files with an API that works at the document and page level while still exposing lower-level PDF objects when needed.

## Target framework

`ZingPDF` currently targets `.NET 8`.

## What it can do

- Load an existing PDF from a seekable stream
- Create a new blank PDF
- Read page counts and access individual pages
- Append, insert, delete, and merge pages
- Rotate pages across a document
- Extract text
- Access and update AcroForm data
- Add simple text watermarks
- Compress and decompress stream content
- Save incremental updates
- Authenticate encrypted PDFs
- Encrypt a plain PDF or remove encryption from an encrypted PDF

## Quick start

### Load an existing PDF

```csharp
using var input = File.OpenRead("input.pdf");
using var pdf = Pdf.Load(input);

var pageCount = await pdf.GetPageCountAsync();
var firstPage = await pdf.GetPageAsync(1);
```

### Create a blank PDF

```csharp
using var pdf = Pdf.Create(options =>
{
    options.MediaBox = Rectangle.FromDimensions(595, 842);
});

using var output = File.Create("blank.pdf");
await pdf.SaveAsync(output);
```

### Append, insert, and delete pages

```csharp
using var input = File.OpenRead("input.pdf");
using var pdf = Pdf.Load(input);

await pdf.AppendPageAsync(options =>
{
    options.MediaBox = Rectangle.FromDimensions(595, 842);
});

await pdf.InsertPageAsync(1, options =>
{
    options.MediaBox = Rectangle.FromDimensions(300, 300);
});

await pdf.DeletePageAsync(2);

using var output = File.Create("pages-updated.pdf");
await pdf.SaveAsync(output);
```

### Merge another PDF

```csharp
using var input = File.OpenRead("input.pdf");
using var pdf = Pdf.Load(input);

using var appendix = File.OpenRead("appendix.pdf");
await pdf.AppendPdfAsync(appendix);

using var output = File.Create("merged.pdf");
await pdf.SaveAsync(output);
```

### Extract text

```csharp
using var input = File.OpenRead("input.pdf");
using var pdf = Pdf.Load(input);

var textItems = await pdf.ExtractTextAsync();

foreach (var item in textItems)
{
    Console.WriteLine(item.Text);
}
```

### Add a watermark

```csharp
using var input = File.OpenRead("input.pdf");
using var pdf = Pdf.Load(input);

await pdf.AddWatermarkAsync("DRAFT");

using var output = File.Create("watermarked.pdf");
await pdf.SaveAsync(output);
```

### Compress stream content

```csharp
using var input = File.OpenRead("input.pdf");
using var pdf = Pdf.Load(input);

pdf.Compress(dpi: 72, quality: 75);

using var output = File.Create("compressed.pdf");
await pdf.SaveAsync(output);
```

### Decompress stream content

```csharp
using var input = File.OpenRead("input.pdf");
using var pdf = Pdf.Load(input);

await pdf.DecompressAsync();

using var output = File.Create("decompressed.pdf");
await pdf.SaveAsync(output);
```

## Encryption

### Open an encrypted PDF

```csharp
using var input = File.OpenRead("encrypted.pdf");
using var pdf = Pdf.Load(input);

await pdf.AuthenticateAsync("password");

var pageCount = await pdf.GetPageCountAsync();
```

### Encrypt a previously unencrypted PDF

```csharp
using var pdf = Pdf.Create();

await pdf.EncryptAsync("user-password", "owner-password");

using var output = File.Create("protected.pdf");
await pdf.SaveAsync(output);
```

### Remove encryption from an encrypted PDF

```csharp
using var input = File.OpenRead("encrypted.pdf");
using var pdf = Pdf.Load(input);

await pdf.DecryptAsync("password");

using var output = File.Create("decrypted.pdf");
await pdf.SaveAsync(output);
```

## Forms

If the document contains an AcroForm, `GetFormAsync()` returns a `Form` wrapper that can be used to read or update fields before saving.

```csharp
using var input = File.OpenRead("form.pdf");
using var pdf = Pdf.Load(input);

var form = await pdf.GetFormAsync();

if (form is not null)
{
    // Read or update fields through the Form API.
}

using var output = File.Create("form-updated.pdf");
await pdf.SaveAsync(output);
```

## Save behavior

ZingPDF saves by writing an incremental update.

That means:

- The input stream passed to `Pdf.Load(...)` must be seekable.
- The output stream passed to `SaveAsync(...)` must be writable and seekable.
- If you save to a different stream, that stream must be empty.
- If you save back to the original stream, ZingPDF appends the update to that same PDF.

Typical pattern:

```csharp
using var input = File.OpenRead("input.pdf");
using var pdf = Pdf.Load(input);

await pdf.AddWatermarkAsync("INTERNAL");

using var output = File.Create("output.pdf");
await pdf.SaveAsync(output);
```

## Important implementation notes

- Encryption currently writes Standard security handler encryption using RC4 (`V=2`, `R=3`).
- `DecryptAsync(...)` removes encryption in the latest saved revision. Because saves are incremental, older encrypted revisions may still exist in the file bytes.
- `Compress(...)` currently compresses unfiltered streams and can recompress JPEG image streams.
- `DecompressAsync()` skips JPEG image streams so they are not corrupted by forced decompression.
- `InsertPageAsync(...)` inserts before the requested page number. To add to the end of the document, use `AppendPageAsync(...)`.

## Low-level access

If you need to work below the page/form abstraction layer, `IPdf.Objects` exposes the PDF object collection and page tree.

This is useful for advanced scenarios, but most application code should prefer the higher-level `Pdf` and `Page` APIs first.

## Status

The library now has end-to-end coverage for:

- page insertion and append behavior
- blank PDF creation
- watermark generation
- compression
- encrypted save and decrypt workflows

Areas still worth documenting or expanding further in future:

- richer form examples
- metadata APIs
- signing
- a full rewrite save mode for users who need old revisions physically removed
