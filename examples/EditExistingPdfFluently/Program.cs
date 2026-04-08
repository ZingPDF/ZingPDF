using ZingPDF;
using ZingPDF.Graphics;

Directory.CreateDirectory("output");

var sourcePath = Path.Combine("output", "pages-source.pdf");
var editedPath = Path.Combine("output", "pages-edited.pdf");

await Pdf.New()
    .Page(page => page
        .Size(595, 842)
        .Text(text => text
            .Value("Northwind ERP Modernisation")
            .HelveticaBold()
            .FontSize(24)
            .At(48, 780)))
    .Page(page => page
        .Size(595, 842)
        .Text(text => text
            .Value("Second page to remove")
            .HelveticaBold()
            .FontSize(24)
            .At(48, 780)))
    .SaveToFileAsync(sourcePath);

using var pdf = Pdf.Load(File.OpenRead(sourcePath));

await pdf.Pages(pages => pages
        .Page(1, page => page
            .Rectangle(box => box
                .At(48, 700)
                .Size(220, 40)
                .Stroke(RGBColour.PrimaryBlue, 1)
                .Fill(new RGBColour(0.9, 0.97, 1)))
            .Text(text => text
                .Value("Approved for rollout")
                .Helvetica()
                .FontSize(12)
                .InBox(60, 712, 196, 16)
                .AlignStart()
                .AlignMiddle()
                .Padding(0)))
        .Append(page => page
            .Size(595, 842)
            .Text(text => text
                .Value("Appended summary page")
                .HelveticaBold()
                .FontSize(24)
                .At(48, 780))
            .Watermark("EXAMPLE")))
    .SaveToFileAsync(editedPath);
