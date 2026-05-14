<p align="center">
  <a href="https://zingpdf.dev">
    <img src="./website/logo.svg" alt="ZingPDF logo" width="220">
  </a>
</p>

# ZingPDF

[![NuGet](https://img.shields.io/nuget/dt/zingpdf?style=for-the-badge&labelColor=%233bc4f3&color=%233c9ad5)](https://www.nuget.org/packages/ZingPDF/)

[Website](https://zingpdf.dev) · [Docs](https://zingpdf.dev/docs.html) · [Guides](https://zingpdf.dev/guides.html) · [Capabilities](https://zingpdf.dev/capabilities.html) · [Performance](https://zingpdf.dev/performance.html)

ZingPDF is a .NET 8 library for reading, editing, creating, templating, and saving PDF files in C#. It's free for non-commercial use.

- PDF templating from Liquid HTML
- Editing
- Signing
- Document assembly
- Text extraction
- Form filling and flattening
- Watermarking
- Compression
- Metadata updates
- Encryption / Decryption
- Redaction
- History Removal

## Repository layout

- `src/`: core library and companion packages
- `tests/`: unit, integration, smoke, and performance coverage
- `examples/`: small runnable examples for common tasks
- `website/`: product site, docs, guides, and generated API reference
- `scripts/`: release and maintenance scripts

## Documentation

- website: [zingpdf.dev](https://zingpdf.dev)
- docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- guides: [zingpdf.dev/guides.html](https://zingpdf.dev/guides.html)
- capability matrix: [zingpdf.dev/capabilities.html](https://zingpdf.dev/capabilities.html)
- performance comparison: [zingpdf.dev/performance.html](https://zingpdf.dev/performance.html)
- generated API reference: [zingpdf.dev/api/](https://zingpdf.dev/api/)
- examples folder: [examples](./examples)
- support and compatibility notes: [docs/project/SUPPORT.md](./docs/project/SUPPORT.md)

## Packages

- `src/ZingPDF`: core PDF APIs
- `src/ZingPDF.Templates`: shared template abstractions
- `src/ZingPDF.Templates.LiquidHtml`: Liquid HTML template rendering package
- `src/ZingPDF.GoogleFonts`: optional Google Fonts integration package
- `src/ZingPDF.OCR`: OCR support for image-based PDF pages
- `src/ZingPDF.FromHTML`: HTML-to-PDF helpers

## Licensing

ZingPDF is proprietary software. Commercial use requires an active subscription or a separate commercial agreement. See [LICENSE.txt](./LICENSE.txt) for the current license terms.
