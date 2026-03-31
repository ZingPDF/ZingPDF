using System.Text;
using System.Text.RegularExpressions;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Parsing;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax;
using ZingPDF;
using ZingPDF.FromHTML;
using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics;
using ZingPDF.Elements;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Extensions;
using System;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Text;
using ZingPDF.Fonts;
using ZingPDF.GoogleFonts;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using DrawingPath = ZingPDF.Elements.Drawing.Path;
using IOPath = System.IO.Path;

//using var outputFileStream = new FileStream("output.pdf", FileMode.Create);
//var pdf = new Pdf();

//pdf.AppendPage();

//await pdf.WriteAsync(outputFileStream);

//await CreateNewPdfAndValidate("output.pdf");

//await ParseResaveValidate("Spec/ISO_32000-2-2020.pdf", "output.pdf");
//await ParseResaveValidate("testfiles/pdf/generated-image-heavy.pdf", "output.pdf");
//await ParseResaveValidate("testfiles/pdf/GS9_Color_Management.pdf", "output.pdf");
//await ParseResaveValidate("output.pdf", "output2.pdf");
//await ParseResaveValidate("testfiles/pdf/form.pdf", "output.pdf");
//await ParseResaveValidate("testfiles/pdf/test.pdf", "output.pdf");

//await AppendPdf("testfiles/pdf/minimal3.pdf", "testfiles/pdf/minimal.pdf", "output.pdf");
//await AppendPdf("testfiles/pdf/minimal.pdf", "testfiles/pdf/form.pdf", "output.pdf");
//await AppendPdf("testfiles/pdf/test.pdf", "testfiles/pdf/form.pdf", "output.pdf");
//await AppendPdf("testfiles/pdf/combobox-form.pdf", "testfiles/pdf/test.pdf", "output.pdf");
//await AppendPdf("testfiles/pdf/minimal.pdf", "testfiles/pdf/minimal2.pdf", "output.pdf");
//await Parse("testfiles/pdf/minimal.pdf");
//await Parse("testfiles/pdf/minimal3.pdf");
//await Parse("testfiles/pdf/test.pdf");
//await Parse("testfiles/pdf/form.pdf");
//await Parse("output.pdf");
//await Parse("testfiles/pdf/generated-text-heavy.pdf");
//await Parse("testfiles/pdf/encrypted.pdf");

//await ConvertFromHTML(new Uri("https://www.google.com"), "output.pdf");
//await ConvertFromHTMLContent("testfiles/html/form-test.html", "form-test.pdf");

//await AddPage("testfiles/pdf/test.pdf", "output.pdf");

//await AddTextToPage();

//await AddImageToPage();

await RunRecentFeatureManualTestsAsync();
//await RemoveHistory();

//await RotatePage();

//await RotateWholeDocument();
//await Watermark("testfiles/pdf/test.pdf", "output.pdf", "CONFIDENTIAL");

//await CompleteForm("testfiles/pdf/complex-form.pdf", "output.pdf");
//await CompleteForm("testfiles/pdf/combobox-form.pdf", "output.pdf");

//await WipeFields();

//await TempFieldApTest();

//await Test();

//await Decompress("testfiles/pdf/form.pdf", "decompressed-form.pdf");
//await Decompress("testfiles/pdf/generated-mixed-workload.pdf", "decompressed-generated-mixed-workload.pdf");
//await Decompress("testfiles/pdf/combobox-form.pdf", "decompressed-combobox-form.pdf");

//await ExtractText("testfiles/pdf/generated-text-heavy.pdf");
//await ExtractText("testfiles/pdf/combobox-form.pdf");

//var test = new CrossReferenceEntry(0, 0, true, true);

static async Task RunRecentFeatureManualTestsAsync()
{
    var outputDirectory = IOPath.Combine(AppContext.BaseDirectory, "manual-output");
    Directory.CreateDirectory(outputDirectory);
    var runStamp = CreateManualTestRunStamp();

    Console.WriteLine($"Writing manual test artifacts to {outputDirectory}");

    await ManualTest_AddTextWithFontOptionsAsync(outputDirectory, runStamp);
    await ManualTest_TextLayoutOptionsAsync(outputDirectory, runStamp);
    await ManualTest_AddImagesAsync(outputDirectory, runStamp);
    await ManualTest_AddPathsAsync(outputDirectory, runStamp);
    await ManualTest_RegisterStandardFontAsync(outputDirectory, runStamp);

    var localTrueTypeFontPath = TryGetLocalTrueTypeFontPath();
    if (localTrueTypeFontPath is not null)
    {
        await ManualTest_RegisterTrueTypeFontsAsync(outputDirectory, runStamp, localTrueTypeFontPath);
    }
    else
    {
        Console.WriteLine("Skipping embedded TrueType font registration tests because no local .ttf font was found.");
    }

    var googleFontsApiKey = TryGetGoogleFontsApiKey();
    if (!string.IsNullOrWhiteSpace(googleFontsApiKey))
    {
        await ManualTest_RegisterGoogleFontAsync(outputDirectory, runStamp, googleFontsApiKey);
    }
    else
    {
        Console.WriteLine("Skipping Google Fonts registration test because GOOGLE_FONTS_API_KEY is not set.");
    }
}

static string CreateManualTestRunStamp()
    => DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");

static async Task RemoveHistory()
{
    using var inputFileStream = new FileStream("testfiles/pdf/generated-incremental-history.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);
    var pdf = Pdf.Load(inputFileStream);
    await pdf.RemoveHistoryAsync();
    await pdf.SaveAsync(outputFileStream);
}

static async Task Watermark(string input, string output, string text)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    await pdf.AddWatermarkAsync(text);

    await pdf.SaveAsync(outputFileStream);
}

static async Task ExtractText(string input)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    var pdf = Pdf.Load(inputFileStream);

    var extract = await pdf.ExtractTextAsync();

    extract.ToList().ForEach(x =>
    {
        Console.WriteLine($"Page: {x.PageNumber}, Font: {x.FontName}, Text: {x.Text}, X: {x.X}, Y: {x.Y}");
        Console.WriteLine();
    });
}

static async Task Decompress(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    await pdf.DecompressAsync();

    await pdf.SaveAsync(outputFileStream);
}

static async Task WipeFields()
{
    using var inputFileStream = new FileStream("testfiles/pdf/combobox-form.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("blank-form.pdf", FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    var form = await pdf.GetFormAsync();

    var fields = await form!.GetFieldsAsync();

    var textFields = fields.OfType<TextFormField>();

    foreach (var field in textFields)
    {
        await field.ClearAsync();
    }

    await pdf.SaveAsync(outputFileStream);
}

static async Task TempFieldApTest()
{
    using var inputFileStream = new FileStream("test.pdf", FileMode.Open);

    var pdf = Pdf.Load(inputFileStream);

    var form = await pdf.GetFormAsync();

    var fields = await form!.GetFieldsAsync();

    var textFields = fields.Where(x => x is TextFormField textField);

    foreach (var field in textFields)
    {
        var apStream = await ((TextFormField)field).GetAPAsync();

        if (apStream == null)
        {
            continue;
        }

        using var ms = new MemoryStream();

        await apStream.WriteAsync(ms);

        ms.Position = 0;

        Console.WriteLine($"Field Size: {await field.GetFieldDimensionsAsync()}");
        Console.WriteLine($"Field AP: {await ms.GetAsync()}");
    }
}

static async Task Test()
{
    //using var inputFileStream = new FileStream("testfiles/pdf/generated-text-heavy.pdf", FileMode.Open);
    //using var inputFileStream = new FileStream("testfiles/pdf/complex-form.pdf", FileMode.Open);
    //using var inputFileStream = new FileStream("testfiles/pdf/combobox-form.pdf", FileMode.Open);
    using var inputFileStream = new FileStream("testfiles/pdf/encrypted.pdf", FileMode.Open);

    var pdf = Pdf.Load(inputFileStream);

    //var encrypted = pdf.Encrypted;

    var count = await pdf.GetPageCountAsync();
}

static async Task Parse(string input)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);

    var pdf = Pdf.Load(inputFileStream);

    var pageCount = await pdf.GetPageCountAsync();
}

static async Task AppendPdf(string input1, string input2, string output)
{
    using var inputFileStream1 = new FileStream(input1, FileMode.Open);
    using var inputFileStream2 = new FileStream(input2, FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = Pdf.Load(inputFileStream1);

    await pdf.AppendPdfAsync(inputFileStream2);

    await pdf.SaveAsync(outputFileStream);
}

static async Task CompleteForm(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    var form = await pdf.GetFormAsync()!;
    
    var fields = await form.GetFieldsAsync();

    foreach (var field in fields)
    {
        if (field is TextFormField textField)
        {
            await textField.SetValueAsync("test");
        }
        else if (field is CheckboxFormField cbField)
        {
            var options = await cbField.GetOptionsAsync();

            await options[0].SelectAsync();
        }
        else if (field is RadioButtonFormField rbField)
        {
            var options = await rbField.GetOptionsAsync();

            await options[0].SelectAsync();
        }
        else if (field is ListBoxFormField listBoxFormField)
        {
            var options = await listBoxFormField.GetOptionsAsync();

            await options[3].SelectAsync();
        }
        else if (field is ComboBoxFormField comboBoxFormField)
        {
            var options = await comboBoxFormField.GetOptionsAsync();

            //options[1].Select();
            await comboBoxFormField.SelectCustomValueAsync("TEST");
        }
    }

    await pdf.SaveAsync(outputFileStream);
}

static async Task AddTextToPage()
{
    using var inputFileStream = new FileStream("test.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    var page = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromDimensions(200, 200));

    await page.AddTextAsync(new ZingPDF.Text.TextObject(
        "test",
        Rectangle.FromDimensions(200, 200),
        new ZingPDF.Text.FontOptions
        {
            ResourceName = "Helv",
            Size = 24,
            Colour = RGBColour.PrimaryRed
        }));

    await pdf.SaveAsync(outputFileStream);
}

static async Task RotateWholeDocument()
{
    using var inputFileStream = new FileStream("testfiles/pdf/generated-mixed-workload.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    await pdf.SetRotationAsync(Rotation.Degrees90);

    await pdf.SaveAsync(outputFileStream);
}

static async Task RotatePage()
{
    using var inputFileStream = new FileStream("testfiles/pdf/test.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    var page = await pdf.GetPageAsync(1);

    page.RotateAsync(Rotation.Degrees90);

    await pdf.SaveAsync(outputFileStream);
}

static async Task AddImageToPage()
{
    using var inputFileStream = new FileStream("testfiles/pdf/minimal.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    var page = await pdf.GetPageAsync(1);

    //var page = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromSize(200, 200));

    await page.AddImageAsync(Image.FromFile("testfiles/image/cat.jpg", Rectangle.FromDimensions(200, 200)));

    await pdf.SaveAsync(outputFileStream);
}

static async Task ManualTest_AddTextWithFontOptionsAsync(string outputDirectory, string runStamp)
{
    using var inputFileStream = OpenTestAsset("test.pdf");
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-text-font-options.pdf", runStamp);

    var pdf = Pdf.Load(inputFileStream);
    var page = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromDimensions(400, 220));

    await page.AddTextAsync(
        "FontOptions overload using an existing page font resource. jumping glyphs prove descenders stay visible.",
        Rectangle.FromCoordinates(new Coordinate(24, 120), new Coordinate(376, 184)),
        new FontOptions
        {
            ResourceName = "Helv",
            Size = 20,
            Colour = RGBColour.PrimaryBlue
        });

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static async Task ManualTest_TextLayoutOptionsAsync(string outputDirectory, string runStamp)
{
    using var pdf = Pdf.Create();
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-text-layout-options.pdf", runStamp);
    var page = await pdf.GetPageAsync(1);
    var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);

    await page.AddTextAsync(
        "Visible overflow keeps descenders intact and lets the line run naturally beyond the box if needed.",
        Rectangle.FromCoordinates(new Coordinate(24, 180), new Coordinate(300, 232)),
        font,
        18,
        RGBColour.Black);

    await page.AddPathAsync(new DrawingPath(
        new StrokeOptions(new RGBColour(0.75, 0.75, 0.75), 1),
        null,
        PathType.Linear,
        [
            new Coordinate(24, 180),
            new Coordinate(300, 180),
            new Coordinate(300, 232),
            new Coordinate(24, 232),
            new Coordinate(24, 180)
        ]));

    await page.AddTextAsync(
        "Clip overflow keeps everything inside the box, including the end of the sentence.",
        Rectangle.FromCoordinates(new Coordinate(24, 110), new Coordinate(300, 162)),
        font,
        18,
        RGBColour.PrimaryBlue,
        new TextLayoutOptions
        {
            Overflow = TextOverflowMode.Clip
        });

    await page.AddPathAsync(new DrawingPath(
        new StrokeOptions(new RGBColour(0.75, 0.75, 0.75), 1),
        null,
        PathType.Linear,
        [
            new Coordinate(24, 110),
            new Coordinate(300, 110),
            new Coordinate(300, 162),
            new Coordinate(24, 162),
            new Coordinate(24, 110)
        ]));

    await page.AddTextAsync(
        "Shrink to fit keeps the full sentence visible within the available width.",
        Rectangle.FromCoordinates(new Coordinate(24, 40), new Coordinate(300, 92)),
        font,
        18,
        RGBColour.PrimaryRed,
        new TextLayoutOptions
        {
            Overflow = TextOverflowMode.ShrinkToFit
        });

    await page.AddPathAsync(new DrawingPath(
        new StrokeOptions(new RGBColour(0.75, 0.75, 0.75), 1),
        null,
        PathType.Linear,
        [
            new Coordinate(24, 40),
            new Coordinate(300, 40),
            new Coordinate(300, 92),
            new Coordinate(24, 92),
            new Coordinate(24, 40)
        ]));

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static async Task ManualTest_AddImagesAsync(string outputDirectory, string runStamp)
{
    using var pdf = Pdf.Create();
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-images-jpg-and-png.pdf", runStamp);
    var page = await pdf.GetPageAsync(1);

    await page.AddImageAsync(
        ResolveTestAssetPath(IOPath.Combine("testfiles", "image", "cat.jpg")),
        Rectangle.FromCoordinates(new Coordinate(24, 24), new Coordinate(204, 204)));

    await using var pngStream = await CreateSamplePngAsync();
    await page.AddImageAsync(
        pngStream,
        Rectangle.FromCoordinates(new Coordinate(220, 40), new Coordinate(380, 140)),
        preserveAspectRatio: false);

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static async Task ManualTest_AddPathsAsync(string outputDirectory, string runStamp)
{
    using var pdf = Pdf.Create();
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-drawing-paths.pdf", runStamp);
    var page = await pdf.GetPageAsync(1);

    await page.AddPathAsync(new DrawingPath(
        new StrokeOptions(RGBColour.PrimaryRed, 3),
        null,
        PathType.Linear,
        [
            new Coordinate(30, 30),
            new Coordinate(120, 130),
            new Coordinate(200, 50),
            new Coordinate(280, 140)
        ]));

    await page.AddPathAsync(new DrawingPath(
        new StrokeOptions(RGBColour.PrimaryBlue, 2),
        new FillOptions(new RGBColour(0.78, 0.9, 1)),
        PathType.Bezier,
        [
            new Coordinate(60, 210),
            new Coordinate(110, 310),
            new Coordinate(220, 310),
            new Coordinate(280, 210)
        ]));

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static async Task ManualTest_RegisterStandardFontAsync(string outputDirectory, string runStamp)
{
    using var pdf = Pdf.Create();
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-registered-standard-font.pdf", runStamp);
    var page = await pdf.GetPageAsync(1);
    var standardFont = await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold);

    await page.AddTextAsync(
        "Registered standard font: Helvetica Bold",
        Rectangle.FromCoordinates(new Coordinate(40, 120), new Coordinate(520, 180)),
        standardFont,
        28,
        RGBColour.Black);

    await page.AddTextAsync(
        "This sample should be clearly visible near the middle of the page.",
        Rectangle.FromCoordinates(new Coordinate(40, 84), new Coordinate(540, 128)),
        standardFont,
        16,
        RGBColour.PrimaryBlue);

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static async Task ManualTest_RegisterTrueTypeFontsAsync(string outputDirectory, string runStamp, string fontPath)
{
    using var pdf = Pdf.Create();
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-registered-truetype-fonts.pdf", runStamp);

    var firstPage = await pdf.GetPageAsync(1);
    var pathFont = await pdf.RegisterTrueTypeFontAsync(fontPath);

    await firstPage.AddTextAsync(
        $"Registered from file path: {IOPath.GetFileName(fontPath)}",
        Rectangle.FromCoordinates(new Coordinate(24, 124), new Coordinate(560, 184)),
        pathFont,
        24,
        RGBColour.PrimaryRed);

    await firstPage.AddTextAsync(
        "Embedded TrueType font sample rendered from a local file.",
        Rectangle.FromCoordinates(new Coordinate(24, 88), new Coordinate(560, 128)),
        pathFont,
        15,
        RGBColour.Black);

    var secondPage = await pdf.AppendPageAsync();
    await using var fontStream = File.OpenRead(fontPath);
    var streamFont = await pdf.RegisterTrueTypeFontAsync(fontStream);

    await secondPage.AddTextAsync(
        "Registered from a supplied font stream.",
        Rectangle.FromCoordinates(new Coordinate(24, 124), new Coordinate(520, 184)),
        streamFont,
        24,
        RGBColour.PrimaryBlue);

    await secondPage.AddTextAsync(
        "Second page verifies the stream-based registration path.",
        Rectangle.FromCoordinates(new Coordinate(24, 88), new Coordinate(520, 128)),
        streamFont,
        15,
        RGBColour.Black);

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static async Task ManualTest_RegisterGoogleFontAsync(string outputDirectory, string runStamp, string apiKey)
{
    using var pdf = Pdf.Create();
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-registered-google-font.pdf", runStamp);
    var page = await pdf.GetPageAsync(1);
    var client = new GoogleFontsClient(apiKey);
    var font = await pdf.RegisterGoogleFontAsync(
        client,
        new GoogleFontRequest
        {
            Family = "Inter",
            Variant = "regular",
            PreferVariableFont = true
        });

    await page.AddTextAsync(
        "Registered from Google Fonts.",
        Rectangle.FromCoordinates(new Coordinate(24, 124), new Coordinate(420, 184)),
        font,
        24,
        RGBColour.Black);

    await page.AddTextAsync(
        "If this page renders text, Google Fonts registration is working.",
        Rectangle.FromCoordinates(new Coordinate(24, 88), new Coordinate(560, 128)),
        font,
        15,
        RGBColour.PrimaryBlue);

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static FileStream CreateOutputPdf(string outputDirectory, string fileName, string runStamp)
{
    var baseName = IOPath.GetFileNameWithoutExtension(fileName);
    var extension = IOPath.GetExtension(fileName);
    var stampedBaseName = $"{baseName}-{runStamp}";

    for (var attempt = 0; attempt < 20; attempt++)
    {
        var candidateName = attempt == 0
            ? $"{stampedBaseName}{extension}"
            : $"{stampedBaseName}-{attempt + 1}{extension}";
        var candidatePath = IOPath.Combine(outputDirectory, candidateName);

        try
        {
            return new FileStream(candidatePath, FileMode.Create, FileAccess.Write, FileShare.None);
        }
        catch (IOException) when (File.Exists(candidatePath))
        {
        }
    }

    throw new IOException($"Unable to create an output file for '{fileName}' in '{outputDirectory}'.");
}

static FileStream OpenTestAsset(string relativePath)
    => new(ResolveTestAssetPath(relativePath), FileMode.Open, FileAccess.Read, FileShare.Read);

static string ResolveTestAssetPath(string relativePath)
    => IOPath.Combine(AppContext.BaseDirectory, relativePath);

static async Task<MemoryStream> CreateSamplePngAsync()
{
    using var image = new SixLabors.ImageSharp.Image<Rgba32>(120, 80, new Rgba32(36, 132, 228, 220));
    var output = new MemoryStream();
    await image.SaveAsync(output, new PngEncoder());
    output.Position = 0;
    return output;
}

static string? TryGetGoogleFontsApiKey()
    => Environment.GetEnvironmentVariable("GOOGLE_FONTS_API_KEY")
        ?? Environment.GetEnvironmentVariable("ZINGPDF_GOOGLE_FONTS_API_KEY");

static string? TryGetLocalTrueTypeFontPath()
{
    var candidatePaths = new[]
    {
        ResolveTestAssetPath(IOPath.Combine("testfiles", "font", "NotoSans-Regular.ttf")),
        IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"),
        IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf"),
        IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "calibri.ttf"),
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf",
        "/Library/Fonts/Arial.ttf",
        "/System/Library/Fonts/Supplemental/Arial.ttf"
    };

    return candidatePaths.FirstOrDefault(File.Exists);
}

static async Task ConvertFromHTML(Uri uri, string output)
{
    using var outputFileStream = new FileStream(output, FileMode.Create);

    using var pdfStream = await Converter.ToPdfAsync(uri);

    await pdfStream.CopyToAsync(outputFileStream);
}

static async Task ConvertFromHTMLContent(string htmlFilePath, string output)
{
    using var inputFileStream = new FileStream(htmlFilePath, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var htmlContent = await inputFileStream.ReadToEndAsync();

    using var pdfStream = await Converter.ToPdfAsync(Encoding.UTF8.GetString(htmlContent));

    await pdfStream.CopyToAsync(outputFileStream);
}

static async Task AddPage(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    var count1 = await pdf.GetPageCountAsync();

    var page = await pdf.AppendPageAsync();

    var count2 = await pdf.GetPageCountAsync();

    await pdf.SaveAsync(outputFileStream);
}

static async Task ParseResaveValidate(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    var pdf = Pdf.Load(inputFileStream);

    //var test = await pdf.IndirectObjects.GetAsync(new IndirectObjectReference(new IndirectObjectId(17, 0)));

    var page1 = await pdf.GetPageAsync(1);

    //var annots = (await pdf.IndirectObjects.GetAsync<ArrayObject>(page1.Get<PageDictionary>().Annots)).Cast<IndirectObjectReference>();

    //var annotObjs = new List<IPdfObject>();

    //foreach(var ior in annots)
    //{
    //    var obj = await pdf.IndirectObjects.GetAsync(ior);
    //    annotObjs.Add(obj);
    //}

    //var count1 = await pdf.GetPageCountAsync();

    //await pdf.InsertPageAsync(2, new Page.PageCreationOptions { MediaBox = new Rectangle(new(0, 0), new(200, 200)) });
    //await pdf.DeletePageAsync(1);

    //var count2 = await pdf.GetPageCountAsync();

    //await pdf.AppendPageAsync();

    //var count2 = await pdf.GetPageCountAsync();

    //var test = await pdf.GetPageAsync(1);
    //var test2 = await pdf.GetPageAsync(2);

    using var outputFileStream = new FileStream(output, FileMode.Create);

    await pdf.SaveAsync(outputFileStream);

    Console.WriteLine($"Parsed {input} to {output} with ZingPdf");
}

