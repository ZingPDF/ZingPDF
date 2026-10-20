# Homepage workflow evidence

The downloadable projects use the published NuGet package ZingPDF 1.0.59,
not a project reference to the working tree. Project sources are retained
beside the archives. `project-report/Program.cs` is copied from the repository's
`examples/CreateProjectStatusReport/Program.cs`.

From the repository root, with dotnet, Python + pypdf, and Poppler installed:

```powershell
pwsh ./scripts/update-homepage-examples.ps1
```

The script packages only source files, extracts fresh copies into a unique
ignored `.tools/homepage-examples` directory, and runs both projects there.
It copies the generated PDFs and text into the website, checks page counts and
page-specific text with pypdf, writes `verification.json`, and renders previews.
Inspect every page before publishing; the command retains second-page previews
and build outputs in the run directory. Both projects need the .NET 8 SDK or a
later SDK with the .NET 8 runtime installed. The first restore needs internet.

These examples demonstrate generated, text-based inputs and a fixed-layout
report. They do not establish customer-corpus compatibility, OCR accuracy,
production capacity, or portability to untested deployment environments.
