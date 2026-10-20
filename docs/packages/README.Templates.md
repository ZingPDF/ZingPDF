![ZingPDF logomark](https://raw.githubusercontent.com/ZingPDF/ZingPDF/main/docs/packages/logomark.svg)

# ZingPDF.Templates

`ZingPDF.Templates` contains shared contracts and diagnostics for ZingPDF template renderer packages. It is the dependency-light base package for template sources, diagnostics, and renderer-specific packages.

Install a renderer package such as `ZingPDF.Templates.LiquidHtml` to create PDFs from templates.


## .NET compatibility

Targets `net8.0`, with .NET 8 as the minimum. Runtime validation covers .NET 8,
9, and 10 on Windows. .NET Framework 4.x and applications targeting .NET 6 or 7
are not supported. Native dependencies and deployment requirements remain package-specific. See [runtime and platform requirements](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/SUPPORT.md).

## Installation

```bash
dotnet add package ZingPDF.Templates
```

## Main workflows

- share template sources between renderer packages
- report template parse, render, and conversion diagnostics
- build renderer packages without taking a dependency on HTML, Liquid, or Chromium
- pair with `ZingPDF.Templates.LiquidHtml` when you want Liquid HTML templates that render to PDF

## More information

- core docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- repository: [github.com/ZingPDF/ZingPDF](https://github.com/ZingPDF/ZingPDF)
