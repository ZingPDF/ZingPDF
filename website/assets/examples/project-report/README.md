# Generate a two-page delivery report

Requires the .NET 8 SDK or a later SDK with the .NET 8 runtime installed.
This project restores ZingPDF 1.0.59 from NuGet. First restore needs internet access.

The library also supports applications targeting .NET 9 and 10. To run this
example on one of those runtimes, change `<TargetFramework>net8.0</TargetFramework>`
in `ProjectReport.csproj` to `net9.0` or `net10.0`, and use the matching SDK/runtime.
The library package itself can remain targeted at `net8.0`.

Unzip the project, open a terminal in its folder, and run:

```sh
dotnet run --project ProjectReport.csproj
```

The program writes `output/project-status-report.pdf`. Its source is copied from
`examples/CreateProjectStatusReport/Program.cs` in the ZingPDF repository.
It uses fictional report data, standard PDF fonts, text, shapes, and explicit
page coordinates. There is no HTML renderer or browser requirement for this example.

The homepage artifact check requires two pages and the headings
`Northwind ERP Modernisation` and `Operational detail`, using pypdf independently
of ZingPDF. Both rendered pages are also inspected before publication.
The report uses a fixed layout; it does not demonstrate automatic pagination
for arbitrary-length customer content. Test your own documents before deployment.

Evaluation and non-commercial use are free. Commercial use requires paid
coverage under the terms at https://zingpdf.dev/commercial-terms.html.
