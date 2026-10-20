# Create, edit, and read a PDF

Requires the .NET 8 SDK or a later SDK with the .NET 8 runtime installed.
This project restores ZingPDF 1.0.59 from NuGet. First restore needs internet access.

The library also supports applications targeting .NET 9 and 10. To run this
example on one of those runtimes, change `<TargetFramework>net8.0</TargetFramework>`
in `FirstPdf.csproj` to `net9.0` or `net10.0`, and use the matching SDK/runtime.
The library package itself can remain targeted at `net8.0`.

Unzip the project, open a terminal in its folder, and run:

```sh
dotnet run --project FirstPdf.csproj
```

The program creates `output/original.pdf`, opens that file, adds an approval
note to page 1, appends a checklist page, and saves `output/edited.pdf`.
It reopens the saved PDF, requires exactly two pages, and requires all three
expected text strings in the extracted text. Failure exits with an exception.
The extracted text is written to `output/extracted-text.txt`.

The input is a generated PDF with embedded text. This example does not test OCR
or compatibility with an arbitrary PDF corpus. Test your own documents before
deployment. Public page numbers are 1-based. Loaded PDFs require an open,
seekable input stream; saving to another stream requires an empty, writable,
seekable output stream. The file helpers manage those output streams here.

Evaluation and non-commercial use are free. Commercial use requires paid
coverage under the terms at https://zingpdf.dev/commercial-terms.html.
