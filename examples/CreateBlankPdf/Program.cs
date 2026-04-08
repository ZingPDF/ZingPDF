using ZingPDF;
using ZingPDF.Graphics;

Directory.CreateDirectory("output");

await Pdf.New()
    .Page(page => page
        .Size(595, 842)
        .Text(text => text
            .Value("Hello from ZingPDF")
            .HelveticaBold()
            .FontSize(24)
            .At(72, 760))
        .Line(line => line
            .From(72, 744)
            .To(252, 744)
            .Stroke(RGBColour.PrimaryBlue, 2))
        .Rectangle(box => box
            .At(72, 700)
            .Size(220, 44)
            .Stroke(RGBColour.PrimaryBlue, 2)
            .Fill(new RGBColour(0.9, 0.97, 1)))
        .Watermark("EXAMPLE"))
    .SaveToFileAsync(Path.Combine("output", "blank-pdf-example.pdf"));
