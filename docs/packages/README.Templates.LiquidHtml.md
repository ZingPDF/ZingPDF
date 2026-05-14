![ZingPDF logomark](https://raw.githubusercontent.com/ZingPDF/ZingPDF/main/docs/packages/logomark.svg)

# ZingPDF.Templates.LiquidHtml

`ZingPDF.Templates.LiquidHtml` creates PDFs from Liquid HTML templates. It uses Fluid for Liquid rendering and `ZingPDF.FromHTML` for HTML-to-PDF conversion.

## Installation

```bash
dotnet add package ZingPDF.Templates.LiquidHtml
```

## Quick start

Render a Liquid HTML template file to PDF:

```csharp
using ZingPDF.Templates.LiquidHtml;

var invoice = new
{
    Number = "INV-1001",
    CustomerName = "Ada Lovelace",
    Items = new[]
    {
        new { Description = "Consulting", Total = 240m },
        new { Description = "Support", Total = 80m }
    }
};

await using var output = File.Create("invoice.pdf");

await LiquidHtmlPdfTemplate
    .FromFile("invoice.liquid.html")
    .RenderAsync(invoice, output);
```

The template can use Liquid variables, loops, and conditionals:

```liquid
<!doctype html>
<html>
  <body>
    <h1>Invoice {{ Number }}</h1>
    <p>{{ CustomerName }}</p>

    <table>
      {% for item in Items %}
      <tr>
        <td>{{ item.Description }}</td>
        <td>{{ item.Total }}</td>
      </tr>
      {% endfor %}
    </table>
  </body>
</html>
```

## Notes

- This package depends on `ZingPDF.FromHTML`, Fluid, and the browser automation environment required by PuppeteerSharp.
- File-backed templates resolve Liquid includes from the template directory by default.
- Use `RenderHtmlAsync(...)` to inspect rendered HTML before PDF conversion.

## More information

- core docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- Liquid HTML template guide: [zingpdf.dev/create-pdf-from-liquid-html-template-csharp.html](https://zingpdf.dev/create-pdf-from-liquid-html-template-csharp.html)
- repository: [github.com/ZingPDF/ZingPDF](https://github.com/ZingPDF/ZingPDF)
