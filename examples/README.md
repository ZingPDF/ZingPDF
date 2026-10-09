# Examples

Small runnable examples for common ZingPDF tasks, including the fluent authoring API.

## Projects

- `CreateBlankPdf`: create a new PDF with `Pdf.New()`, add text and shapes, and save it
- `EditExistingPdfFluently`: load a PDF, edit an existing page, append one, remove one, and save through `pdf.Pages(...)`
- `CreateProjectStatusReport`: create a polished two-page delivery report using `Pdf.New()`, boxed text, cards, and table-style layout
- `FillAndFlattenForm`: load an AcroForm PDF, fill fields by name, flatten the form, and save it
- `ExportSelectedPages`: copy selected pages into a new PDF and save the result

- `GenerateInvoices`: render Liquid invoices with 0, 1, 40 and 500 rows through a shared, explicitly installed Chromium browser; check repeated headers, row text, totals and embedded fonts. Includes a Linux container recipe.

## Invoice rendering

See [GenerateInvoices](./GenerateInvoices) for browser installation and execution commands. Set `ZINGPDF_BROWSER_PATH` to the installed executable. The renderer defaults to print media and CSS page sizes, with at most four concurrent isolated browser contexts. Embed assets for offline page rendering. Validate the recipe in the target Linux image.

## Run

```bash
dotnet run --project .\examples\CreateBlankPdf\CreateBlankPdf.csproj
dotnet run --project .\examples\CreateProjectStatusReport\CreateProjectStatusReport.csproj
dotnet run --project .\examples\FillAndFlattenForm\FillAndFlattenForm.csproj
dotnet run --project .\examples\ExportSelectedPages\ExportSelectedPages.csproj
```

Each sample writes its output into an `output` folder.
