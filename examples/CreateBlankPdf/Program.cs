using ZingPDF;
using ZingPDF.Fonts;
using ZingPDF.Graphics;
using ZingPDF.Syntax.CommonDataStructures;

Directory.CreateDirectory("output");

using var pdf = Pdf.Create();
var page = await pdf.GetPageAsync(1);
var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);

await page.AddTextAsync(
    "Hello from ZingPDF",
    Rectangle.FromDimensions(320, 72),
    font,
    18,
    RGBColour.Black);

await using var output = File.Create(Path.Combine("output", "blank-pdf-example.pdf"));
await pdf.SaveAsync(output);
