# Examples

Small runnable examples for common ZingPDF tasks.

## Projects

- `CreateBlankPdf`: create a new PDF, register a standard font, add text, and save it
- `FillAndFlattenForm`: load an AcroForm PDF, fill fields by name, flatten the form, and save it
- `ExportSelectedPages`: copy selected pages into a new PDF and save the result

## Run

```bash
dotnet run --project .\examples\CreateBlankPdf\CreateBlankPdf.csproj
dotnet run --project .\examples\FillAndFlattenForm\FillAndFlattenForm.csproj
dotnet run --project .\examples\ExportSelectedPages\ExportSelectedPages.csproj
```

Each sample writes its output into its own `output` folder under the project directory.
