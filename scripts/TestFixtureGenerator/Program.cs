using ZingPDF;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Text;

var root = Directory.GetCurrentDirectory();
var pdfDir = System.IO.Path.Combine(root, "TestFiles", "pdf");
var imagePath = System.IO.Path.Combine(root, "TestFiles", "image", "cat.jpg");
var basePdfPath = System.IO.Path.Combine(root, "TestFiles", "pdf", "test.pdf");
var basePdfPath2 = System.IO.Path.Combine(root, "TestFiles", "pdf", "minimal3.pdf");

Directory.CreateDirectory(pdfDir);

await GenerateMinimalFixtureAsync(System.IO.Path.Combine(pdfDir, "minimal.pdf"));
await GenerateTextHeavyFixtureAsync(System.IO.Path.Combine(pdfDir, "generated-text-heavy.pdf"), basePdfPath, basePdfPath2);
await GenerateImageHeavyFixtureAsync(System.IO.Path.Combine(pdfDir, "generated-image-heavy.pdf"), imagePath);
await GenerateIncrementalHistoryFixtureAsync(System.IO.Path.Combine(pdfDir, "generated-incremental-history.pdf"));
await GenerateMixedWorkloadFixtureAsync(System.IO.Path.Combine(pdfDir, "generated-mixed-workload.pdf"), imagePath);

static async Task GenerateMinimalFixtureAsync(string outputPath)
{
    using var pdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(595.276, 841.890));
    await SaveFreshAsync(pdf, outputPath);
}

static async Task GenerateTextHeavyFixtureAsync(string outputPath, string basePdfPath, string basePdfPath2)
{
    using var input = File.OpenRead(basePdfPath);
    using var pdf = Pdf.Load(input);
    var appendStreams = new List<MemoryStream>();

    try
    {
        for (var iteration = 0; iteration < 12; iteration++)
        {
            var source = (iteration & 1) == 0 ? basePdfPath : basePdfPath2;
            var bytes = await File.ReadAllBytesAsync(source);
            var appendStream = new MemoryStream(bytes, writable: false);
            appendStreams.Add(appendStream);
            await pdf.AppendPdfAsync(appendStream);
        }

        var metadata = await pdf.GetMetadataAsync();
        metadata.Title = "Generated text-heavy workload";
        metadata.Author = "ZingPDF fixture generator";

        await SaveFreshAsync(pdf, outputPath);
    }
    finally
    {
        foreach (var stream in appendStreams)
        {
            await stream.DisposeAsync();
        }
    }
}

static async Task GenerateImageHeavyFixtureAsync(string outputPath, string imagePath)
{
    using var pdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(595, 842));
    var openImages = new List<Image>();
    var imageBytes = await File.ReadAllBytesAsync(imagePath);
    var headingFont = await pdf.RegisterStandardFontAsync("Helvetica-Bold", "FixtureHead");

    try
    {
        for (var pageIndex = 0; pageIndex < 12; pageIndex++)
        {
            var page = pageIndex == 0
                ? await pdf.GetPageAsync(1)
                : await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(595, 842));

            await page.AddTextAsync(new TextObject(
                $"Image workload page {pageIndex + 1}",
                Rectangle.FromCoordinates(new Coordinate(40, 24), new Coordinate(320, 60)),
                headingFont.CreateOptions(18, RGBColour.Black)));

            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 2; column++)
                {
                    var left = 40 + (column * 255);
                    var top = 90 + (row * 220);

                    var image = new Image(
                        new MemoryStream(imageBytes, writable: false),
                        Rectangle.FromCoordinates(new Coordinate(left, top), new Coordinate(left + 220, top + 180)));
                    openImages.Add(image);

                    await page.AddImageAsync(image);
                }
            }
        }

        await SaveFreshAsync(pdf, outputPath);
    }
    finally
    {
        foreach (var image in openImages)
        {
            image.Dispose();
        }
    }
}

static async Task GenerateIncrementalHistoryFixtureAsync(string outputPath)
{
    byte[] currentBytes;

    await using (var initialOutput = new MemoryStream())
    {
        using var pdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(595, 842));
        var headingFont = await pdf.RegisterStandardFontAsync("Helvetica", "FixtureHead");
        var page = await pdf.GetPageAsync(1);

        await page.AddTextAsync(new TextObject(
            "Incremental history base revision",
            Rectangle.FromCoordinates(new Coordinate(40, 80), new Coordinate(400, 120)),
            headingFont.CreateOptions(20, RGBColour.Black)));

        await pdf.SaveAsync(initialOutput);
        currentBytes = initialOutput.ToArray();
    }

    for (var revision = 1; revision <= 2; revision++)
    {
        await using var input = new MemoryStream(currentBytes, writable: false);
        using var pdf = Pdf.Load(input);
        var metadata = await pdf.GetMetadataAsync();
        metadata.Title = $"Incremental fixture revision {revision}";
        metadata.Author = "ZingPDF fixture generator";
        metadata.Creator = "ZingPDF fixture generator";
        metadata.Subject = $"Incremental fixture revision {revision}";

        await using var revisionOutput = new MemoryStream();
        await pdf.SaveAsync(revisionOutput);
        currentBytes = revisionOutput.ToArray();
    }

    await File.WriteAllBytesAsync(outputPath, currentBytes);
}

static async Task GenerateMixedWorkloadFixtureAsync(string outputPath, string imagePath)
{
    var authoring = Pdf.New();

    for (var pageIndex = 0; pageIndex < 8; pageIndex++)
    {
        var capturedPageIndex = pageIndex;

        authoring.Page(page =>
        {
            page.Size(595, 842);

            page.Text(text => text
                .Value($"Mixed workload page {capturedPageIndex + 1}")
                .HelveticaBold()
                .FontSize(18)
                .Color(RGBColour.PrimaryRed)
                .At(40, 800));

            page.Text(text => text
                .Value(BuildParagraph(capturedPageIndex, 99))
                .Helvetica()
                .FontSize(12)
                .Color(RGBColour.Black)
                .InBox(40, 610, 515, 150)
                .AlignStart()
                .AlignTop()
                .Padding(0)
                .Wrap());

            page.Image(image => image
                .FromFile(imagePath)
                .At(40, 322)
                .Size(260, 260));

            page.Text(text => text
                .Value("This fixture mixes text blocks and JPEG image XObjects to exercise parsing, extraction, and save paths.")
                .Helvetica()
                .FontSize(12)
                .Color(RGBColour.Black)
                .InBox(320, 410, 235, 120)
                .AlignStart()
                .AlignTop()
                .Padding(0)
                .Wrap());
        });
    }

    await authoring.SaveToFileAsync(outputPath);
}

static async Task SaveFreshAsync(Pdf pdf, string outputPath)
{
    await using var output = File.Create(outputPath);
    await pdf.SaveAsync(output);
}

static string BuildParagraph(int pageIndex, int blockIndex)
{
    var words = new[]
    {
        "invoice", "statement", "approval", "archive", "metadata", "extract", "merge", "rotate",
        "signature", "checkbox", "annotation", "revision", "history", "compress", "decrypt", "catalog"
    };

    var builder = new System.Text.StringBuilder();
    builder.Append($"Fixture page {pageIndex + 1}, block {blockIndex + 1}. ");

    for (var sentence = 0; sentence < 5; sentence++)
    {
        builder.Append("ZingPDF test content ");
        for (var i = 0; i < 12; i++)
        {
            var word = words[(pageIndex + blockIndex + sentence + i) % words.Length];
            builder.Append(word);
            builder.Append(' ');
        }

        builder.Append("for parser and extraction coverage. ");
    }

    return builder.ToString();
}
