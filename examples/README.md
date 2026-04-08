# Examples

Small runnable examples for common ZingPDF tasks, including the fluent authoring API.

## Projects

- `CreateBlankPdf`: create a new PDF with `Pdf.New()`, add text and shapes, and save it
- `EditExistingPdfFluently`: load a PDF, edit an existing page, append one, remove one, and save through `pdf.Pages(...)`
- `CreateProjectStatusReport`: create a polished two-page delivery report using `Pdf.New()`, boxed text, cards, and table-style layout
- `FillAndFlattenForm`: load an AcroForm PDF, fill fields by name, flatten the form, and save it
- `ExportSelectedPages`: copy selected pages into a new PDF and save the result

## Run

```bash
dotnet run --project .\examples\CreateBlankPdf\CreateBlankPdf.csproj
dotnet run --project .\examples\CreateProjectStatusReport\CreateProjectStatusReport.csproj
dotnet run --project .\examples\FillAndFlattenForm\FillAndFlattenForm.csproj
dotnet run --project .\examples\ExportSelectedPages\ExportSelectedPages.csproj
```

Each sample writes its output into an `output` folder.
