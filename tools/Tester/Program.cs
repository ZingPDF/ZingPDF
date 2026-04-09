using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ZingPDF;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Fonts;
using ZingPDF.Graphics;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Text;
using DrawingPath = ZingPDF.Elements.Drawing.Path;
using IOPath = System.IO.Path;

await RunCapabilityValidationSamplesAsync();

static async Task RunCapabilityValidationSamplesAsync()
{
    var outputDirectory = IOPath.Combine(AppContext.BaseDirectory, "capability-validation");
    Directory.CreateDirectory(outputDirectory);
    foreach (var existingPdf in Directory.EnumerateFiles(outputDirectory, "*.pdf"))
    {
        try
        {
            File.Delete(existingPdf);
        }
        catch (IOException)
        {
        }
    }

    Console.WriteLine($"Writing capability validation PDFs to {outputDirectory}");

    await CreateCapabilitySample_AuthoringAsync(IOPath.Combine(outputDirectory, "01-authoring.pdf"));
    await CreateCapabilitySample_RedactionAsync(IOPath.Combine(outputDirectory, "02-redaction.pdf"));
    await CreateCapabilitySample_PageEditingAsync(IOPath.Combine(outputDirectory, "03-page-editing.pdf"));
    await CreateCapabilitySample_PageOperationsAsync(IOPath.Combine(outputDirectory, "04-page-operations.pdf"));
    await CreateCapabilitySample_ExportPagesAsync(IOPath.Combine(outputDirectory, "05-export-pages.pdf"));
    await CreateCapabilitySample_RotationAsync(IOPath.Combine(outputDirectory, "06-rotation.pdf"));
    await CreateCapabilitySample_MetadataAsync(IOPath.Combine(outputDirectory, "07-metadata.pdf"));
    await CreateCapabilitySample_TextExtractionAsync(IOPath.Combine(outputDirectory, "08-text-extraction.pdf"));
    await CreateCapabilitySample_FontsAsync(IOPath.Combine(outputDirectory, "09-fonts.pdf"));
    await CreateCapabilitySample_FormCreationAsync(IOPath.Combine(outputDirectory, "10-form-creation.pdf"));
    await CreateCapabilitySample_FormsFillAsync(IOPath.Combine(outputDirectory, "11-form-completion.pdf"));
    await CreateCapabilitySample_FormsFlattenAsync(IOPath.Combine(outputDirectory, "12-forms-flatten.pdf"));
    await CreateCapabilitySample_SigningAsync(IOPath.Combine(outputDirectory, "13-signing.pdf"));
    await CreateCapabilitySample_EncryptionAsync(IOPath.Combine(outputDirectory, "14-encryption.pdf"));
    await CreateCapabilitySample_DecryptAsync(IOPath.Combine(outputDirectory, "15-decrypt.pdf"));
    await CreateCapabilitySample_IncrementalSaveAsync(IOPath.Combine(outputDirectory, "16-incremental-save.pdf"));
    await CreateCapabilitySample_RemoveHistoryAsync(IOPath.Combine(outputDirectory, "17-remove-history.pdf"));
    await CreateCapabilitySample_PackageSetupPdfAsync(
        IOPath.Combine(outputDirectory, "18-google-fonts.pdf"),
        "Google Fonts package",
        TryGetGoogleFontsApiKey() is { Length: > 0 }
            ? "A Google Fonts API key is available in this environment. The next pass should replace this setup note with a real rendered sample that uses WithGoogleFont(...)."
            : "No Google Fonts API key is configured. Set GOOGLE_FONTS_API_KEY or ZINGPDF_GOOGLE_FONTS_API_KEY, rerun Tester, and replace this setup note with a real rendered sample.");
    await CreateCapabilitySample_PackageSetupPdfAsync(
        IOPath.Combine(outputDirectory, "19-ocr.pdf"),
        "OCR package",
        "This environment still needs a validated OCR setup sample. Once Tesseract and tessdata are available, replace this setup note with a scanned-page example and include the extracted text on the first page for comparison.");
    await CreateCapabilitySample_InvisibleSigningAsync(IOPath.Combine(outputDirectory, "20-invisible-signing.pdf"));
    await CreateCapabilitySample_EncryptionPermissionsAsync(IOPath.Combine(outputDirectory, "21-encryption-permissions.pdf"));
    await CreateCapabilitySample_MalformedRecoveryAsync(IOPath.Combine(outputDirectory, "22-malformed-recovery.pdf"));
    await CreateCapabilitySample_SignatureImageAsync(IOPath.Combine(outputDirectory, "23-signature-image.pdf"));
    await CreateCapabilitySample_TextOnlyRedactionAsync(IOPath.Combine(outputDirectory, "24-text-redaction-safety.pdf"));
    await CreateCapabilitySample_FormRoundTripAsync(IOPath.Combine(outputDirectory, "25-form-roundtrip.pdf"));
    await CreateCapabilitySample_ComplexVectorAsync(IOPath.Combine(outputDirectory, "26-complex-vector.pdf"));
}

static async Task CreateCapabilitySample_AuthoringAsync(string outputPath)
{
    using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

    await Pdf.New()
        .Page(page => page
            .Size(595, 842)
            .Text(text => text
                .Value("PDF authoring")
                .HelveticaBold()
                .FontSize(24)
                .At(40, 790))
            .Rectangle(box => box
                .At(40, 660)
                .Size(515, 96)
                .Stroke(new RGBColour(0.78, 0.84, 0.92), 1)
                .Fill(new RGBColour(0.96, 0.98, 1.0)))
            .Text(text => text
                .Value("Manual validation instructions:\n1. You should see this instruction panel at the top of the page.\n2. Below it, you should see a blue title, a horizontal divider line, and a filled accent box.\n3. The page should also contain wrapped body text inside the pale box near the bottom.\n4. Nothing on the page should overlap or run off the page edges.")
                .Helvetica()
                .FontSize(12)
                .InBox(56, 672, 480, 70)
                .Wrap()
                .ClipOverflow())
            .Text(text => text
                .Value("Quarterly delivery summary")
                .HelveticaBold()
                .FontSize(28)
                .Color(RGBColour.PrimaryBlue)
                .At(40, 610))
            .Line(line => line
                .From(40, 596)
                .To(555, 596)
                .Stroke(RGBColour.PrimaryBlue, 2))
            .Rectangle(box => box
                .At(40, 500)
                .Size(180, 60)
                .Fill(new RGBColour(0.88, 0.95, 1.0))
                .Stroke(new RGBColour(0.67, 0.81, 0.94), 1))
            .Text(text => text
                .Value("Status: On track")
                .HelveticaBold()
                .FontSize(18)
                .At(56, 522))
            .Rectangle(box => box
                .At(40, 300)
                .Size(515, 150)
                .Stroke(new RGBColour(0.85, 0.88, 0.93), 1))
            .Text(text => text
                .Value("This sample is exercising the fluent authoring APIs for new PDFs. It combines positioned text, line drawing, filled rectangles, bounded text, and text wrapping in a single document so you can quickly confirm the output looks intentional and stable.")
                .Helvetica()
                .FontSize(14)
                .InBox(56, 322, 483, 106)
                .Wrap()
                .ClipOverflow()))
        .SaveAsync(output);
}

static async Task CreateCapabilitySample_RedactionAsync(string outputPath)
{
    using var source = new MemoryStream();

    await Pdf.New()
        .Page(page => page
            .Size(595, 842)
            .Text(text => text
                .Value("Redaction")
                .HelveticaBold()
                .FontSize(24)
                .At(40, 790))
            .Rectangle(box => box
                .At(40, 620)
                .Size(515, 140)
                .Stroke(new RGBColour(0.78, 0.84, 0.92), 1)
                .Fill(new RGBColour(0.96, 0.98, 1.0)))
            .Text(text => text
                .Value("Manual validation instructions:\n1. You should see two black redaction boxes labeled REDACTED.\n2. You should not see the original blue box or the original text value SECRET-PROJECT-ALPHA.\n3. If you inspect the saved PDF bytes, the original text should not be present.\n4. This output should represent a rewritten file, not an incremental update that preserves the redacted content in history.")
                .Helvetica()
                .FontSize(12)
                .InBox(56, 634, 480, 108)
                .Wrap()
                .ClipOverflow())
            .Rectangle(box => box
                .At(60, 380)
                .Size(180, 80)
                .Fill(RGBColour.PrimaryBlue))
            .Text(text => text
                .Value("SECRET-PROJECT-ALPHA")
                .HelveticaBold()
                .FontSize(18)
                .At(60, 530)))
        .SaveAsync(source);

    source.Position = 0;
    using var pdf = Pdf.Load(source);
    using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

    var plan = await pdf.RedactionAsync();
    plan.MarkRegion(
        1,
        Rectangle.FromCoordinates(
            new Coordinate(58, 378),
            new Coordinate(250, 462)));

    await plan.MarkTextAsync("SECRET-PROJECT-ALPHA");
    await plan.ApplyAsync(new PdfRedactionOptions
    {
        OverlayText = "REDACTED"
    });

    await pdf.SaveAsync(output);
}

static async Task CreateCapabilitySample_PageEditingAsync(string outputPath)
{
    using var source = new MemoryStream();
    using var result = new MemoryStream();

    await Pdf.New()
        .Page(page => page
            .Size(240, 180)
            .Text(text => text.Value("first page").HelveticaBold().FontSize(18).At(20, 140)))
        .Page(page => page
            .Size(240, 180)
            .Text(text => text.Value("second page").HelveticaBold().FontSize(18).At(20, 140)))
        .SaveAsync(source);

    source.Position = 0;
    using var pdf = Pdf.Load(source);

    await pdf.Pages(pages => pages
            .Page(1, page => page
                .Text(text => text.Value("edited").Helvetica().FontSize(12).At(20, 100)))
            .Append(page => page
                .Size(240, 180)
                .Text(text => text.Value("appended page").HelveticaBold().FontSize(18).At(20, 140))))
        .SaveAsync(result);

    await WrapWithInstructionPageAsync(
        outputPath,
        "Fluent page editing",
        "Manual validation instructions:\n1. Page 2 of this output should still contain 'first page' and should also include the added word 'edited'.\n2. Page 3 should contain 'second page'.\n3. Page 4 should contain 'appended page'.\n4. This validates pdf.Pages(...) against an existing loaded document.",
        result);
}

static async Task CreateCapabilitySample_PageOperationsAsync(string outputPath)
{
    using var source = new MemoryStream();
    using var mergeSource = new MemoryStream();
    using var result = new MemoryStream();

    await Pdf.New()
        .Page(page => page.Size(240, 180).Text(text => text.Value("alpha").HelveticaBold().FontSize(18).At(20, 140)))
        .Page(page => page.Size(240, 180).Text(text => text.Value("bravo").HelveticaBold().FontSize(18).At(20, 140)))
        .SaveAsync(source);

    await Pdf.New()
        .Page(page => page.Size(240, 180).Text(text => text.Value("merged page").HelveticaBold().FontSize(18).At(20, 140)))
        .SaveAsync(mergeSource);

    source.Position = 0;
    mergeSource.Position = 0;
    using var pdf = Pdf.Load(source);

    var insertedPage = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromDimensions(240, 180));
    await insertedPage.AddTextAsync(
        "inserted page",
        Rectangle.FromCoordinates(new Coordinate(20, 120), new Coordinate(200, 150)),
        await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold),
        18,
        RGBColour.Black);

    await pdf.DeletePageAsync(3);
    await pdf.AppendPdfAsync(mergeSource);
    await pdf.SaveAsync(result);

    await WrapWithInstructionPageAsync(
        outputPath,
        "Append, insert, delete, and merge pages",
        "Manual validation instructions:\n1. Page 2 should say 'inserted page'.\n2. Page 3 should say 'alpha'.\n3. The original 'bravo' page should be gone.\n4. The final page should say 'merged page'.",
        result);
}

static async Task CreateCapabilitySample_ExportPagesAsync(string outputPath)
{
    using var source = new MemoryStream();
    using var result = new MemoryStream();

    await Pdf.New()
        .Page(page => page.Size(240, 180).Text(text => text.Value("page one").HelveticaBold().FontSize(18).At(20, 140)))
        .Page(page => page.Size(240, 180).Text(text => text.Value("page two").HelveticaBold().FontSize(18).At(20, 140)))
        .Page(page => page.Size(240, 180).Text(text => text.Value("page three").HelveticaBold().FontSize(18).At(20, 140)))
        .SaveAsync(source);

    source.Position = 0;
    using var pdf = Pdf.Load(source);
    using var exported = await pdf.ExportPagesAsync([3, 1]);
    await exported.SaveAsync(result);

    await WrapWithInstructionPageAsync(
        outputPath,
        "Export selected pages",
        "Manual validation instructions:\n1. Page 2 should contain 'page three'.\n2. Page 3 should contain 'page one'.\n3. The exported document should contain only those selected pages in that order.",
        result);
}

static async Task CreateCapabilitySample_RotationAsync(string outputPath)
{
    using var source = new MemoryStream();
    using var result = new MemoryStream();

    await Pdf.New()
        .Page(page => page.Size(240, 180).Text(text => text.Value("rotate ninety").HelveticaBold().FontSize(18).At(20, 140)))
        .Page(page => page.Size(240, 180).Text(text => text.Value("rotate one eighty").HelveticaBold().FontSize(18).At(20, 140)))
        .SaveAsync(source);

    source.Position = 0;
    using var pdf = Pdf.Load(source);
    await (await pdf.GetPageAsync(1)).RotateAsync(Rotation.Degrees90);
    await (await pdf.GetPageAsync(2)).RotateAsync(Rotation.Degrees180);
    await pdf.SaveAsync(result);

    await WrapWithInstructionPageAsync(
        outputPath,
        "Page rotation",
        "Manual validation instructions:\n1. Page 2 should be rotated 90 degrees.\n2. Page 3 should be rotated 180 degrees.\n3. The page content itself should still render, just with the updated page rotation values.",
        result);
}

static async Task CreateCapabilitySample_MetadataAsync(string outputPath)
{
    using var instructionDocument = await CreateInstructionDocumentAsync(
        "Document metadata",
        "Manual validation instructions:\n1. Open the PDF document properties in your viewer.\n2. Title should be 'Capability Validation Metadata'.\n3. Author should be 'ZingPDF Tester'.\n4. Subject and keywords should also be populated.",
        "This file exists mainly for property inspection rather than visual rendering.");

    using var pdf = Pdf.Load(instructionDocument);
    var metadata = await pdf.GetMetadataAsync();
    metadata.Title = "Capability Validation Metadata";
    metadata.Author = "ZingPDF Tester";
    metadata.Subject = "Metadata validation sample";
    metadata.Keywords = "metadata,validation,zingpdf";
    metadata.Creator = "Tester";

    using var output = OpenValidationOutput(outputPath);
    await pdf.SaveAsync(output);
}

static async Task CreateCapabilitySample_TextExtractionAsync(string outputPath)
{
    using var source = new MemoryStream();
    await Pdf.New()
        .Page(page => page
            .Size(240, 180)
            .Text(text => text.Value("Line one").Helvetica().FontSize(16).At(20, 140))
            .Text(text => text.Value("Line two").Helvetica().FontSize(16).At(20, 110)))
        .SaveAsync(source);

    source.Position = 0;
    using var pdf = Pdf.Load(source);
    var extracted = string.Join("\n", (await pdf.ExtractTextAsync(1)).Select(x => x.Text));

    using var summary = await CreateInstructionDocumentAsync(
        "Text extraction",
        "Manual validation instructions:\n1. The summary on this page should include both 'Line one' and 'Line two'.\n2. That summary text was produced by calling ExtractTextAsync on a generated source page.",
        $"Extracted text:\n{extracted}");

    using var output = OpenValidationOutput(outputPath);
    await summary.CopyToAsync(output);
}

static async Task CreateCapabilitySample_FontsAsync(string outputPath)
{
    using var output = OpenValidationOutput(outputPath);
    var trueTypeFontPath = TryGetLocalTrueTypeFontPath()
        ?? throw new InvalidOperationException("No TrueType font is available for the fonts validation sample.");

    await Pdf.New()
        .Page(page => page
            .Size(595, 842)
            .Text(text => text.Value("Fonts").HelveticaBold().FontSize(24).At(40, 790))
            .Rectangle(box => box.At(40, 620).Size(515, 130).Stroke(new RGBColour(0.78, 0.84, 0.92), 1).Fill(new RGBColour(0.96, 0.98, 1.0)))
            .Text(text => text
                .Value("Manual validation instructions:\n1. You should see one line in a standard PDF font and one in an embedded TrueType font.\n2. Inspect the PDF resources if you want to confirm the embedded font stream is present.\n3. Both lines should render cleanly after reload.")
                .Helvetica().FontSize(12).InBox(56, 636, 480, 96).Wrap().ClipOverflow())
            .Text(text => text.Value("Standard font: Helvetica").Font(StandardPdfFonts.Helvetica).FontSize(20).At(56, 520))
            .Text(text => text.Value("Embedded TrueType font: Noto Sans").WithTrueTypeFont(trueTypeFontPath, "NotoSans-Regular").FontSize(20).At(56, 470)))
        .SaveAsync(output);
}

static async Task CreateCapabilitySample_FormCreationAsync(string outputPath)
{
    using var output = OpenValidationOutput(outputPath);

    using (var pdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(595, 842)))
    {
        await AddInstructionPageAsync(
            pdf,
            "Form creation",
            "Manual validation instructions:\n1. Page 2 should contain a blank text field, an unchecked checkbox, two blank radio buttons, a blank combo box, and an empty signature field.\n2. This page should be created through GetOrCreateFormAsync() and the high-level Add...FieldAsync(...) methods, not by writing low-level dictionaries in the sample.");

        var page = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(420, 340));
        var headingFont = await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold);
        var bodyFont = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);

        await page.AddTextAsync(
            "High-level form creation sample",
            Rectangle.FromCoordinates(new Coordinate(30, 302), new Coordinate(300, 326)),
            headingFont,
            18,
            RGBColour.Black);
        await page.AddTextAsync(
            "Customer name",
            Rectangle.FromCoordinates(new Coordinate(30, 266), new Coordinate(170, 284)),
            bodyFont,
            12,
            RGBColour.Black);
        await page.AddTextAsync(
            "Accept terms and confirm that long labels stay readable beside the control.",
            Rectangle.FromCoordinates(new Coordinate(58, 176), new Coordinate(330, 204)),
            bodyFont,
            12,
            RGBColour.Black,
            new TextLayoutOptions { Wrap = true, Overflow = TextOverflowMode.Clip });
        await page.AddTextAsync("Delivery option", Rectangle.FromCoordinates(new Coordinate(30, 142), new Coordinate(150, 160)), bodyFont, 12, RGBColour.Black);
        await page.AddTextAsync(
            "Standard delivery (3-5 business days)",
            Rectangle.FromCoordinates(new Coordinate(58, 112), new Coordinate(260, 138)),
            bodyFont,
            12,
            RGBColour.Black,
            new TextLayoutOptions { Wrap = true, Overflow = TextOverflowMode.Clip });
        await page.AddTextAsync(
            "Express delivery (next business day)",
            Rectangle.FromCoordinates(new Coordinate(58, 78), new Coordinate(260, 104)),
            bodyFont,
            12,
            RGBColour.Black,
            new TextLayoutOptions { Wrap = true, Overflow = TextOverflowMode.Clip });
        await page.AddTextAsync("Priority", Rectangle.FromCoordinates(new Coordinate(30, 52), new Coordinate(120, 70)), bodyFont, 12, RGBColour.Black);
        await page.AddTextAsync("Approval signature", Rectangle.FromCoordinates(new Coordinate(30, -8), new Coordinate(160, 10)), bodyFont, 12, RGBColour.Black);

        var createdForm = await pdf.GetOrCreateFormAsync();
        await createdForm.AddTextFieldAsync(
            2,
            "CustomerName",
            Rectangle.FromCoordinates(new Coordinate(30, 226), new Coordinate(250, 252)),
            options => options.Description = "Customer name");
        await createdForm.AddCheckboxFieldAsync(
            2,
            "AcceptTerms",
            Rectangle.FromCoordinates(new Coordinate(30, 182), new Coordinate(46, 198)),
            options => options.Description = "Accept terms");
        await createdForm.AddRadioButtonFieldAsync(
            2,
            "DeliveryOption",
            [
                new RadioButtonFieldOption("Standard", Rectangle.FromCoordinates(new Coordinate(30, 122), new Coordinate(46, 138))),
                new RadioButtonFieldOption("Express", Rectangle.FromCoordinates(new Coordinate(30, 88), new Coordinate(46, 104)))
            ],
            options => options.Description = "Delivery option");
        await createdForm.AddComboBoxFieldAsync(
            2,
            "Priority",
            Rectangle.FromCoordinates(new Coordinate(30, 14), new Coordinate(180, 42)),
            [new ChoiceFieldOption("Low"), new ChoiceFieldOption("Medium"), new ChoiceFieldOption("High")],
            options => options.Description = "Priority");
        await createdForm.AddSignatureFieldAsync(
            2,
            "ApprovalSignature",
            Rectangle.FromCoordinates(new Coordinate(30, -32), new Coordinate(250, -10)),
            options => options.Description = "Approval signature");
        await pdf.SaveAsync(output);
    }
}

static async Task CreateCapabilitySample_FormsFillAsync(string outputPath)
{
    using var source = new MemoryStream();

    using (var pdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(595, 842)))
    {
        await AddInstructionPageAsync(
            pdf,
            "Form completion",
            "Manual validation instructions:\n1. Page 2 should show 'validated' in the text field.\n2. The checkbox should be selected.\n3. The 'Express' radio button should be selected.\n4. The combo box should show 'High'.\n5. This validates high-level field enumeration and completion after reloading the created form.");

        var page = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(420, 340));
        var headingFont = await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold);
        var bodyFont = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);

        await page.AddTextAsync(
            "High-level form completion sample",
            Rectangle.FromCoordinates(new Coordinate(30, 302), new Coordinate(320, 326)),
            headingFont,
            18,
            RGBColour.Black);
        await page.AddTextAsync(
            "Customer name",
            Rectangle.FromCoordinates(new Coordinate(30, 266), new Coordinate(170, 284)),
            bodyFont,
            12,
            RGBColour.Black);
        await page.AddTextAsync(
            "Accept terms and confirm that long labels stay readable beside the control.",
            Rectangle.FromCoordinates(new Coordinate(58, 176), new Coordinate(330, 204)),
            bodyFont,
            12,
            RGBColour.Black,
            new TextLayoutOptions { Wrap = true, Overflow = TextOverflowMode.Clip });
        await page.AddTextAsync("Delivery option", Rectangle.FromCoordinates(new Coordinate(30, 142), new Coordinate(150, 160)), bodyFont, 12, RGBColour.Black);
        await page.AddTextAsync(
            "Standard delivery (3-5 business days)",
            Rectangle.FromCoordinates(new Coordinate(58, 112), new Coordinate(260, 138)),
            bodyFont,
            12,
            RGBColour.Black,
            new TextLayoutOptions { Wrap = true, Overflow = TextOverflowMode.Clip });
        await page.AddTextAsync(
            "Express delivery (next business day)",
            Rectangle.FromCoordinates(new Coordinate(58, 78), new Coordinate(260, 104)),
            bodyFont,
            12,
            RGBColour.Black,
            new TextLayoutOptions { Wrap = true, Overflow = TextOverflowMode.Clip });
        await page.AddTextAsync("Priority", Rectangle.FromCoordinates(new Coordinate(30, 52), new Coordinate(120, 70)), bodyFont, 12, RGBColour.Black);

        var createdForm = await pdf.GetOrCreateFormAsync();
        await createdForm.AddTextFieldAsync(
            2,
            "CustomerName",
            Rectangle.FromCoordinates(new Coordinate(30, 226), new Coordinate(250, 252)),
            options => options.Description = "Customer name");
        await createdForm.AddCheckboxFieldAsync(
            2,
            "AcceptTerms",
            Rectangle.FromCoordinates(new Coordinate(30, 182), new Coordinate(46, 198)),
            options => options.Description = "Accept terms");
        await createdForm.AddRadioButtonFieldAsync(
            2,
            "DeliveryOption",
            [
                new RadioButtonFieldOption("Standard", Rectangle.FromCoordinates(new Coordinate(30, 122), new Coordinate(46, 138))),
                new RadioButtonFieldOption("Express", Rectangle.FromCoordinates(new Coordinate(30, 88), new Coordinate(46, 104)))
            ],
            options => options.Description = "Delivery option");
        await createdForm.AddComboBoxFieldAsync(
            2,
            "Priority",
            Rectangle.FromCoordinates(new Coordinate(30, 14), new Coordinate(180, 42)),
            [new ChoiceFieldOption("Low"), new ChoiceFieldOption("Medium"), new ChoiceFieldOption("High")],
            options => options.Description = "Priority");
        await pdf.SaveAsync(source);
    }

    source.Position = 0;
    using var reloadedPdf = Pdf.Load(source);
    var form = await reloadedPdf.GetFormAsync() ?? throw new InvalidOperationException("Expected form.");
    var fields = await form.GetFieldsAsync();

    foreach (var field in fields)
    {
        if (field is TextFormField textField)
        {
            await textField.SetValueAsync("validated");
        }
        else if (field is CheckboxFormField checkboxField)
        {
            await (await checkboxField.GetOptionsAsync()).Single().SelectAsync();
        }
        else if (field is RadioButtonFormField radioField)
        {
            await (await radioField.GetOptionsAsync()).Single(x => x.Value == "Express").SelectAsync();
        }
        else if (field is ComboBoxFormField comboBoxField)
        {
            await comboBoxField.SelectOptionByValueAsync("High");
        }
    }

    using var output = OpenValidationOutput(outputPath);
    await reloadedPdf.SaveAsync(output);
}

static async Task CreateCapabilitySample_FormsFlattenAsync(string outputPath)
{
    using var input = OpenTestAsset(IOPath.Combine("testfiles", "pdf", "combobox-form.pdf"));
    using var result = new MemoryStream();
    using var pdf = Pdf.Load(input);
    var form = await pdf.GetFormAsync() ?? throw new InvalidOperationException("Expected form.");

    foreach (var field in await form.GetFieldsAsync())
    {
        if (field is TextFormField textField)
        {
            await textField.SetValueAsync("flattened");
        }
        else if (field is ComboBoxFormField comboField)
        {
            await comboField.SelectCustomValueAsync("flattened");
        }
    }

    await form.FlattenAsync();
    await pdf.SaveAsync(result);

    await WrapWithInstructionPageAsync(
        outputPath,
        "Form flattening",
        "Manual validation instructions:\n1. The form page after this instruction page should still show the field appearance.\n2. The fields should no longer behave as interactive form controls in a viewer.\n3. This validates flattening after fill.",
        result);
}

static async Task CreateCapabilitySample_SigningAsync(string outputPath)
{
    using var pdf = Pdf.Create();
    await AddInstructionPageAsync(
        pdf,
        "Visible signing",
        "Manual validation instructions:\n1. Page 2 should contain a visible signature appearance.\n2. This sample should use a plain text-based signature appearance rather than a custom image.\n3. In Acrobat, the signature may show as UNKNOWN because this sample uses a self-signed test certificate.\n4. The important validation result is that Acrobat reports the document has not been modified since the signature was applied.",
        pageNumber: 1);

    var signedPage = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(595, 842));
    await signedPage.AddTextAsync(
        "Visible signing sample",
        Rectangle.FromCoordinates(new Coordinate(40, 740), new Coordinate(300, 780)),
        await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold),
        20,
        RGBColour.Black);

    var form = await pdf.GetOrCreateFormAsync();
    var signatureField = await form.AddSignatureFieldAsync(
        2,
        "ApprovalSignature",
        Rectangle.FromCoordinates(new Coordinate(40, 620), new Coordinate(240, 680)),
        options => options.Description = "Approval signature");
    using var certificate = CreateSigningCertificate();

    await signatureField.SignAsync(certificate, new PdfSignatureOptions
    {
        SignerName = "ZingPDF Tester",
        Reason = "Manual validation"
    });

    using var output = OpenValidationOutput(outputPath);
    await pdf.SaveAsync(output);
}

static async Task CreateCapabilitySample_EncryptionAsync(string outputPath)
{
    using var encrypted = new MemoryStream();

    using (var pdf = Pdf.Create())
    {
        var page = await pdf.GetPageAsync(1);
        await page.AddTextAsync(
            "Open this file with password: test123",
            Rectangle.FromCoordinates(new Coordinate(40, 720), new Coordinate(420, 760)),
            await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold),
            18,
            RGBColour.Black);
        await page.AddTextAsync(
            "Validation target: the file should prompt for a password and then open normally.",
            Rectangle.FromCoordinates(new Coordinate(40, 660), new Coordinate(520, 700)),
            await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica),
            12,
            RGBColour.Black,
            new TextLayoutOptions { Wrap = true });
        await pdf.EncryptAsync("test123", "owner123", PdfEncryptionAlgorithm.Aes256, PdfEncryptionPermissions.Print | PdfEncryptionPermissions.FillForms);
        await pdf.SaveAsync(encrypted);
    }

    using var output = OpenValidationOutput(outputPath);
    encrypted.Position = 0;
    await encrypted.CopyToAsync(output);
}

static async Task CreateCapabilitySample_DecryptAsync(string outputPath)
{
    using var encrypted = new MemoryStream();

    using (var sourcePdf = Pdf.Create())
    {
        var sourcePage = await sourcePdf.GetPageAsync(1);
        await sourcePage.AddTextAsync(
            "This page should open without a password.",
            Rectangle.FromCoordinates(new Coordinate(40, 720), new Coordinate(460, 760)),
            await sourcePdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold),
            18,
            RGBColour.Black);
        await sourcePdf.EncryptAsync("test123");
        await sourcePdf.SaveAsync(encrypted);
    }

    encrypted.Position = 0;
    using var pdf = Pdf.Load(encrypted);
    await pdf.DecryptAsync("test123");

    using var result = new MemoryStream();
    await pdf.SaveAsync(result);

    await WrapWithInstructionPageAsync(
        outputPath,
        "Decrypt to a plain latest revision",
        "Manual validation instructions:\n1. The following page should open without prompting for a password.\n2. The decrypted output should contain the original visible text.\n3. This validates decrypting and saving a plain latest revision.",
        result);
}

static async Task CreateCapabilitySample_IncrementalSaveAsync(string outputPath)
{
    using var source = new MemoryStream();
    using var result = new MemoryStream();

    await Pdf.New()
        .Page(page => page.Size(240, 180).Text(text => text.Value("original revision").HelveticaBold().FontSize(18).At(20, 140)))
        .SaveAsync(source);

    source.Position = 0;
    using var pdf = Pdf.Load(source);
    var page = await pdf.GetPageAsync(1);
    await page.AddTextAsync(
        "incremental update",
        Rectangle.FromCoordinates(new Coordinate(20, 100), new Coordinate(180, 130)),
        await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica),
        12,
        RGBColour.PrimaryBlue);
    await pdf.SaveAsync(result);

    await WrapWithInstructionPageAsync(
        outputPath,
        "Incremental save",
        "Manual validation instructions:\n1. The following page should contain both 'original revision' and 'incremental update'.\n2. If you inspect the file bytes, you should see multiple revision structures rather than a fully rewritten file.",
        result);
}

static async Task CreateCapabilitySample_RemoveHistoryAsync(string outputPath)
{
    using var source = new MemoryStream();
    using var history = new MemoryStream();

    using (var initialPdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(595, 842)))
    {
        await AddInstructionPageAsync(
            initialPdf,
            "Document history removal",
            "Manual validation instructions:\n1. Page 2 should still render normally.\n2. If you inspect the saved file bytes near the end of the file, there should be only one cross-reference section, only one final end-of-file marker, and no previous-trailer link.\n3. This validates RemoveHistoryAsync on a document that previously had incremental history.",
            pageNumber: 1);

        var contentPage = await initialPdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(240, 180));
        await contentPage.AddTextAsync(
            "history removed",
            Rectangle.FromCoordinates(new Coordinate(20, 140), new Coordinate(180, 165)),
            await initialPdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold),
            18,
            RGBColour.Black);

        await initialPdf.SaveAsync(source);
    }

    source.Position = 0;
    using (var pdfWithHistory = Pdf.Load(source))
    {
        var metadata = await pdfWithHistory.GetMetadataAsync();
        metadata.Title = "History-bearing remove-history sample";
        await pdfWithHistory.SaveAsync(history);
    }

    history.Position = 0;
    using var rewrittenPdf = Pdf.Load(history);
    await rewrittenPdf.RemoveHistoryAsync();

    using var output = OpenValidationOutput(outputPath);
    await rewrittenPdf.SaveAsync(output);
}

static async Task CreateCapabilitySample_InvisibleSigningAsync(string outputPath)
{
    using var pdf = Pdf.Create();
    await AddInstructionPageAsync(
        pdf,
        "Invisible signing",
        "Manual validation instructions:\n1. There should be no visible signature appearance on any page.\n2. The PDF should still contain a digital signature when you inspect the signature panel in a viewer.\n3. In Acrobat, the signature may show as UNKNOWN because this sample uses a self-signed test certificate.\n4. The important validation result is that the viewer reports the document has not been modified since signing.",
        pageNumber: 1);

    var contentPage = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(595, 842));
    await contentPage.AddTextAsync(
        "Invisible signing sample",
        Rectangle.FromCoordinates(new Coordinate(40, 740), new Coordinate(320, 780)),
        await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold),
        20,
        RGBColour.Black);
    await contentPage.AddTextAsync(
        "This page should remain visually unchanged after signing. Validation happens through the signature panel rather than a visible widget.",
        Rectangle.FromCoordinates(new Coordinate(40, 660), new Coordinate(520, 720)),
        await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica),
        12,
        RGBColour.Black,
        new TextLayoutOptions { Wrap = true, Overflow = TextOverflowMode.Clip });

    using var certificate = CreateSigningCertificate();
    await pdf.SignInvisibleAsync(certificate, new PdfSignatureOptions
    {
        FieldName = "ValidationOnlySignature",
        SignerName = "ZingPDF Tester",
        Reason = "Manual validation"
    });

    using var output = OpenValidationOutput(outputPath);
    await pdf.SaveAsync(output);
}

static async Task CreateCapabilitySample_EncryptionPermissionsAsync(string outputPath)
{
    using var encrypted = new MemoryStream();

    using (var pdf = Pdf.Create())
    {
        var page = await pdf.GetPageAsync(1);
        await page.AddTextAsync(
            "Open this file with password: test123",
            Rectangle.FromCoordinates(new Coordinate(40, 740), new Coordinate(420, 780)),
            await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold),
            18,
            RGBColour.Black);
        await page.AddTextAsync(
            "Manual validation instructions:\n1. Open the file with user password test123.\n2. In the viewer security or document permissions panel, printing should be allowed.\n3. Content copying should be disallowed.\n4. Filling form fields should be allowed.\n5. This validates writing explicit output permissions into the encryption dictionary.",
            Rectangle.FromCoordinates(new Coordinate(40, 600), new Coordinate(540, 720)),
            await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica),
            12,
            RGBColour.Black,
            new TextLayoutOptions { Wrap = true, Overflow = TextOverflowMode.Clip });

        await pdf.EncryptAsync(
            "test123",
            "owner123",
            PdfEncryptionAlgorithm.Aes256,
            PdfEncryptionPermissions.Print | PdfEncryptionPermissions.FillForms);
        await pdf.SaveAsync(encrypted);
    }

    using var output = OpenValidationOutput(outputPath);
    encrypted.Position = 0;
    await encrypted.CopyToAsync(output);
}

static async Task CreateCapabilitySample_MalformedRecoveryAsync(string outputPath)
{
    using var source = new MemoryStream();

    using (var pdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(595, 842)))
    {
        await AddInstructionPageAsync(
            pdf,
            "Malformed PDF recovery",
            "Manual validation instructions:\n1. This output should open and render normally even though the source PDF used to create it had a corrupted final cross-reference pointer.\n2. Page 2 should contain the phrase 'recovered from malformed startxref'.\n3. This validates the recoverable malformed-file-structure path rather than normal clean input parsing.",
            pageNumber: 1);

        var contentPage = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(300, 180));
        await contentPage.AddTextAsync(
            "recovered from malformed startxref",
            Rectangle.FromCoordinates(new Coordinate(20, 120), new Coordinate(280, 150)),
            await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold),
            18,
            RGBColour.Black);

        await pdf.SaveAsync(source);
    }

    var corruptedBytes = CorruptFinalStartXref(source.ToArray());
    using var malformedInput = new MemoryStream(corruptedBytes);
    using var recoveredPdf = Pdf.Load(malformedInput);
    await recoveredPdf.RemoveHistoryAsync();

    using var output = OpenValidationOutput(outputPath);
    await recoveredPdf.SaveAsync(output);
}

static async Task CreateCapabilitySample_SignatureImageAsync(string outputPath)
{
    using var pdf = Pdf.Create();
    await AddInstructionPageAsync(
        pdf,
        "Signature image appearance",
        "Manual validation instructions:\n1. Page 2 should contain a visible signature field.\n2. The signature appearance should clearly include a custom image rather than text alone.\n3. In Acrobat, the signature may show as UNKNOWN because this sample uses a self-signed test certificate.\n4. This validates the custom signature-image appearance path specifically.",
        pageNumber: 1);

    var signedPage = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(595, 842));
    await signedPage.AddTextAsync(
        "Custom signature image sample",
        Rectangle.FromCoordinates(new Coordinate(40, 740), new Coordinate(340, 780)),
        await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold),
        20,
        RGBColour.Black);

    var form = await pdf.GetOrCreateFormAsync();
    var signatureField = await form.AddSignatureFieldAsync(
        2,
        "ImageSignature",
        Rectangle.FromCoordinates(new Coordinate(40, 580), new Coordinate(280, 680)),
        options => options.Description = "Signature field with custom image appearance");

    using var certificate = CreateSigningCertificate();
    await signatureField.SignAsync(certificate, new PdfSignatureOptions
    {
        SignerName = "ZingPDF Tester",
        Reason = "Custom image validation",
        SignatureImageBytes = File.ReadAllBytes(ResolveTestAssetPath(IOPath.Combine("testfiles", "image", "cat.jpg")))
    });

    using var output = OpenValidationOutput(outputPath);
    await pdf.SaveAsync(output);
}

static async Task CreateCapabilitySample_TextOnlyRedactionAsync(string outputPath)
{
    using var source = new MemoryStream();

    await Pdf.New()
        .Page(page => page
            .Size(595, 842)
            .Text(text => text
                .Value("Text-only redaction safety")
                .HelveticaBold()
                .FontSize(24)
                .At(40, 790))
            .Rectangle(box => box
                .At(40, 620)
                .Size(515, 120)
                .Stroke(new RGBColour(0.78, 0.84, 0.92), 1)
                .Fill(new RGBColour(0.96, 0.98, 1.0)))
            .Text(text => text
                .Value("Manual validation instructions:\n1. The secret text below should be replaced by a black REDACTED box.\n2. If you inspect the saved PDF bytes, the original phrase TOP-SECRET-TEXT-ONLY should not be present anywhere.\n3. This validates text-only structural redaction and rewritten output.")
                .Helvetica()
                .FontSize(12)
                .InBox(56, 634, 480, 90)
                .Wrap()
                .ClipOverflow())
            .Text(text => text
                .Value("TOP-SECRET-TEXT-ONLY")
                .HelveticaBold()
                .FontSize(18)
                .At(60, 520)))
        .SaveAsync(source);

    source.Position = 0;
    using var pdf = Pdf.Load(source);
    var plan = await pdf.RedactionAsync();
    await plan.MarkTextAsync("TOP-SECRET-TEXT-ONLY");
    await plan.ApplyAsync(new PdfRedactionOptions { OverlayText = "REDACTED" });

    using var output = OpenValidationOutput(outputPath);
    await pdf.SaveAsync(output);
}

static async Task CreateCapabilitySample_FormRoundTripAsync(string outputPath)
{
    using var source = new MemoryStream();

    using (var pdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(595, 842)))
    {
        await AddInstructionPageAsync(
            pdf,
            "Form round-trip persistence",
            "Manual validation instructions:\n1. Page 2 should show a created form that has been reloaded and then completed.\n2. The text field should contain 'round-tripped'.\n3. The checkbox should be selected and the combo box should show 'Medium'.\n4. This validates high-level form creation, save, reload, completion, and save again in one flow.",
            pageNumber: 1);

        var page = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(420, 260));
        var headingFont = await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold);
        var bodyFont = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);

        await page.AddTextAsync("Round-trip sample", Rectangle.FromCoordinates(new Coordinate(30, 222), new Coordinate(220, 246)), headingFont, 18, RGBColour.Black);
        await page.AddTextAsync("Reference", Rectangle.FromCoordinates(new Coordinate(30, 186), new Coordinate(120, 204)), bodyFont, 12, RGBColour.Black);
        await page.AddTextAsync("Ready for export", Rectangle.FromCoordinates(new Coordinate(58, 132), new Coordinate(230, 150)), bodyFont, 12, RGBColour.Black);
        await page.AddTextAsync("Priority", Rectangle.FromCoordinates(new Coordinate(30, 78), new Coordinate(120, 96)), bodyFont, 12, RGBColour.Black);

        var form = await pdf.GetOrCreateFormAsync();
        await form.AddTextFieldAsync(
            2,
            "Reference",
            Rectangle.FromCoordinates(new Coordinate(30, 146), new Coordinate(230, 172)),
            options => options.Description = "Reference");
        await form.AddCheckboxFieldAsync(
            2,
            "Ready",
            Rectangle.FromCoordinates(new Coordinate(30, 128), new Coordinate(46, 144)),
            options => options.Description = "Ready for export");
        await form.AddComboBoxFieldAsync(
            2,
            "Priority",
            Rectangle.FromCoordinates(new Coordinate(30, 40), new Coordinate(180, 68)),
            [new ChoiceFieldOption("Low"), new ChoiceFieldOption("Medium"), new ChoiceFieldOption("High")],
            options => options.Description = "Priority");

        await pdf.SaveAsync(source);
    }

    source.Position = 0;
    using var reloadedPdf = Pdf.Load(source);
    var reloadedForm = await reloadedPdf.GetFormAsync() ?? throw new InvalidOperationException("Expected round-trip form.");
    foreach (var field in await reloadedForm.GetFieldsAsync())
    {
        if (field is TextFormField textField)
        {
            await textField.SetValueAsync("round-tripped");
        }
        else if (field is CheckboxFormField checkboxField)
        {
            await (await checkboxField.GetOptionsAsync()).Single().SelectAsync();
        }
        else if (field is ComboBoxFormField comboBoxField)
        {
            await comboBoxField.SelectOptionByValueAsync("Medium");
        }
    }

    using var output = OpenValidationOutput(outputPath);
    await reloadedPdf.SaveAsync(output);
}

static async Task CreateCapabilitySample_ComplexVectorAsync(string outputPath)
{
    using var pdf = Pdf.Create();
    await AddInstructionPageAsync(
        pdf,
        "Complex vector illustration",
        "Manual validation instructions:\n1. Page 2 should contain a layered rosette-style vector illustration.\n2. The artwork should be composed of smooth filled and stroked curves rather than raster image pixels.\n3. Petals, rings, and small spark elements should all render crisply when you zoom in.\n4. This validates heavier path-based drawing with many bezier segments on a single page.",
        pageNumber: 1);

    var page = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(595, 842));
    await DrawComplexVectorIllustrationAsync(pdf, page);

    using var output = OpenValidationOutput(outputPath);
    await pdf.SaveAsync(output);
}

static async Task CreateCapabilitySample_PackageSetupPdfAsync(string outputPath, string title, string instructions)
{
    using var summary = await CreateInstructionDocumentAsync(title, instructions, "This PDF is a setup/status artifact for a package-dependent capability.");
    using var output = OpenValidationOutput(outputPath);
    await summary.CopyToAsync(output);
}

static async Task DrawComplexVectorIllustrationAsync(Pdf pdf, Page page)
{
    var navy = new RGBColour(0.08, 0.11, 0.22);
    var deepBlue = new RGBColour(0.14, 0.22, 0.44);
    var cyan = new RGBColour(0.2, 0.74, 0.81);
    var aqua = new RGBColour(0.45, 0.9, 0.88);
    var gold = new RGBColour(0.95, 0.74, 0.3);
    var peach = new RGBColour(0.99, 0.82, 0.62);
    var pink = new RGBColour(0.92, 0.48, 0.6);
    var white = new RGBColour(0.97, 0.98, 1.0);

    var center = new Coordinate(297.5, 380);

    await page.AddPathAsync(new DrawingPath(
        null,
        new FillOptions(navy),
        PathType.Linear,
        [
            new Coordinate(0, 0),
            new Coordinate(595, 0),
            new Coordinate(595, 842),
            new Coordinate(0, 842),
            new Coordinate(0, 0)
        ]));

    await page.AddPathAsync(new DrawingPath(
        null,
        new FillOptions(deepBlue),
        PathType.Linear,
        [
            new Coordinate(48, 130),
            new Coordinate(547, 130),
            new Coordinate(547, 630),
            new Coordinate(48, 630),
            new Coordinate(48, 130)
        ]));

    for (var i = 0; i < 18; i++)
    {
        var angle = i * (Math.PI * 2 / 18);
        var fill = i % 2 == 0 ? cyan : gold;
        await page.AddPathAsync(CreatePetalPath(center, 72, 180, angle, 0.22, fill, white, 2));
    }

    for (var i = 0; i < 12; i++)
    {
        var angle = i * (Math.PI * 2 / 12) + 0.12;
        var fill = i % 2 == 0 ? pink : peach;
        await page.AddPathAsync(CreatePetalPath(center, 48, 124, angle, 0.28, fill, white, 2));
    }

    await page.AddPathAsync(CreateCirclePath(center, 92, new StrokeOptions(aqua, 4), null));
    await page.AddPathAsync(CreateCirclePath(center, 58, new StrokeOptions(white, 3), new FillOptions(deepBlue)));
    await page.AddPathAsync(CreateCirclePath(center, 22, null, new FillOptions(gold)));

    for (var i = 0; i < 24; i++)
    {
        var angle = i * (Math.PI * 2 / 24);
        await page.AddPathAsync(CreateDiamondPath(
            Offset(center, Math.Cos(angle) * 220, Math.Sin(angle) * 220),
            10,
            i % 2 == 0 ? aqua : peach,
            white));
    }

    for (var i = 0; i < 8; i++)
    {
        var angle = i * (Math.PI * 2 / 8) + 0.15;
        var start = Offset(center, Math.Cos(angle) * 26, Math.Sin(angle) * 26);
        var end = Offset(center, Math.Cos(angle) * 74, Math.Sin(angle) * 74);
        await page.AddPathAsync(new DrawingPath(
            new StrokeOptions(white, 2),
            null,
            PathType.Linear,
            [start, end]));
    }

    await page.AddTextAsync(
        "Bezier path stress sample",
        Rectangle.FromCoordinates(new Coordinate(170, 660), new Coordinate(430, 700)),
        await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold),
        22,
        white);
}

static DrawingPath CreatePetalPath(
    Coordinate center,
    double innerRadius,
    double outerRadius,
    double angle,
    double spread,
    RGBColour fillColour,
    RGBColour strokeColour,
    int strokeWidth)
{
    var start = Offset(center, Math.Cos(angle - spread) * innerRadius, Math.Sin(angle - spread) * innerRadius);
    var tip = Offset(center, Math.Cos(angle) * outerRadius, Math.Sin(angle) * outerRadius);
    var end = Offset(center, Math.Cos(angle + spread) * innerRadius, Math.Sin(angle + spread) * innerRadius);

    var control1 = Offset(center, Math.Cos(angle - spread * 0.85) * (innerRadius + outerRadius) * 0.48, Math.Sin(angle - spread * 0.85) * (innerRadius + outerRadius) * 0.48);
    var control2 = Offset(center, Math.Cos(angle - spread * 0.18) * outerRadius * 0.92, Math.Sin(angle - spread * 0.18) * outerRadius * 0.92);
    var control3 = Offset(center, Math.Cos(angle + spread * 0.18) * outerRadius * 0.92, Math.Sin(angle + spread * 0.18) * outerRadius * 0.92);
    var control4 = Offset(center, Math.Cos(angle + spread * 0.85) * (innerRadius + outerRadius) * 0.48, Math.Sin(angle + spread * 0.85) * (innerRadius + outerRadius) * 0.48);

    return new DrawingPath(
        new StrokeOptions(strokeColour, strokeWidth),
        new FillOptions(fillColour),
        PathType.Bezier,
        [start, control1, control2, tip, control3, control4, end]);
}

static DrawingPath CreateCirclePath(Coordinate center, double radius, StrokeOptions? stroke, FillOptions? fill)
{
    const double kappa = 0.5522847498307936;
    var ox = radius * kappa;
    var oy = radius * kappa;

    var top = new Coordinate(center.X, center.Y + radius);
    var right = new Coordinate(center.X + radius, center.Y);
    var bottom = new Coordinate(center.X, center.Y - radius);
    var left = new Coordinate(center.X - radius, center.Y);

    return new DrawingPath(
        stroke,
        fill,
        PathType.Bezier,
        [
            top,
            new Coordinate(center.X + ox, center.Y + radius),
            new Coordinate(center.X + radius, center.Y + oy),
            right,
            new Coordinate(center.X + radius, center.Y - oy),
            new Coordinate(center.X + ox, center.Y - radius),
            bottom,
            new Coordinate(center.X - ox, center.Y - radius),
            new Coordinate(center.X - radius, center.Y - oy),
            left,
            new Coordinate(center.X - radius, center.Y + oy),
            new Coordinate(center.X - ox, center.Y + radius),
            top
        ]);
}

static DrawingPath CreateDiamondPath(Coordinate center, double radius, RGBColour fillColour, RGBColour strokeColour)
{
    return new DrawingPath(
        new StrokeOptions(strokeColour, 1),
        new FillOptions(fillColour),
        PathType.Linear,
        [
            new Coordinate(center.X, center.Y + radius),
            new Coordinate(center.X + radius, center.Y),
            new Coordinate(center.X, center.Y - radius),
            new Coordinate(center.X - radius, center.Y),
            new Coordinate(center.X, center.Y + radius)
        ]);
}

static Coordinate Offset(Coordinate origin, double x, double y) => new(origin.X + x, origin.Y + y);

static async Task<MemoryStream> CreateInstructionDocumentAsync(string title, string instructions, string? summary = null)
{
    var output = new MemoryStream();

    using (var pdf = Pdf.Create(options => options.MediaBox = Rectangle.FromDimensions(595, 842)))
    {
        await AddInstructionPageAsync(pdf, title, instructions, summary);
        await pdf.SaveAsync(output);
    }

    output.Position = 0;
    return output;
}

static async Task AddInstructionPageAsync(Pdf pdf, string title, string instructions, string? summary = null, int pageNumber = 1)
{
    var page = await pdf.GetPageAsync(pageNumber);
    var headingFont = await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold);
    var bodyFont = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);

    await page.AddTextAsync(
        title,
        Rectangle.FromCoordinates(new Coordinate(40, 760), new Coordinate(555, 805)),
        headingFont,
        24,
        RGBColour.Black);

    await page.AddPathAsync(new DrawingPath(
        new StrokeOptions(new RGBColour(0.78, 0.84, 0.92), 1),
        new FillOptions(new RGBColour(0.96, 0.98, 1.0)),
        PathType.Linear,
        [
            new Coordinate(40, 620),
            new Coordinate(555, 620),
            new Coordinate(555, 760),
            new Coordinate(40, 760),
            new Coordinate(40, 620)
        ]));

    await page.AddTextAsync(
        instructions,
        Rectangle.FromCoordinates(new Coordinate(56, 634), new Coordinate(536, 742)),
        bodyFont,
        12,
        RGBColour.Black,
        new TextLayoutOptions { Wrap = true, Overflow = TextOverflowMode.Clip });

    if (!string.IsNullOrWhiteSpace(summary))
    {
        await page.AddPathAsync(new DrawingPath(
            new StrokeOptions(new RGBColour(0.85, 0.88, 0.93), 1),
            null,
            PathType.Linear,
            [
                new Coordinate(40, 470),
                new Coordinate(555, 470),
                new Coordinate(555, 580),
                new Coordinate(40, 580),
                new Coordinate(40, 470)
            ]));

        await page.AddTextAsync(
            summary,
            Rectangle.FromCoordinates(new Coordinate(56, 486), new Coordinate(536, 564)),
            bodyFont,
            13,
            RGBColour.Black,
            new TextLayoutOptions { Wrap = true, Overflow = TextOverflowMode.Clip });
    }
}

static async Task WrapWithInstructionPageAsync(string outputPath, string title, string instructions, params MemoryStream[] appendedDocuments)
{
    using var wrapperStream = await CreateInstructionDocumentAsync(title, instructions);
    using var wrapperPdf = Pdf.Load(wrapperStream);

    foreach (var document in appendedDocuments)
    {
        document.Position = 0;
        await wrapperPdf.AppendPdfAsync(document);
    }

    using var output = OpenValidationOutput(outputPath);
    await wrapperPdf.SaveAsync(output);
}

static FileStream OpenValidationOutput(string outputPath)
{
    var directory = IOPath.GetDirectoryName(outputPath)!;
    Directory.CreateDirectory(directory);

    var baseName = IOPath.GetFileNameWithoutExtension(outputPath);
    var extension = IOPath.GetExtension(outputPath);

    for (var attempt = 0; attempt < 20; attempt++)
    {
        var candidatePath = attempt == 0
            ? outputPath
            : IOPath.Combine(directory, $"{baseName}-{attempt + 1}{extension}");

        try
        {
            return new FileStream(candidatePath, FileMode.Create, FileAccess.Write, FileShare.None);
        }
        catch (IOException) when (File.Exists(candidatePath))
        {
        }
    }

    throw new IOException($"Unable to create validation output '{outputPath}'.");
}

static X509Certificate2 CreateSigningCertificate()
{
    using var rsa = RSA.Create(2048);
    var request = new CertificateRequest(
        "CN=ZingPDF Tester Signer",
        rsa,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);

    request.CertificateExtensions.Add(
        new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

    return request.CreateSelfSigned(
        DateTimeOffset.UtcNow.AddDays(-1),
        DateTimeOffset.UtcNow.AddDays(30));
}


static FileStream OpenTestAsset(string relativePath)
    => new(ResolveTestAssetPath(relativePath), FileMode.Open, FileAccess.Read, FileShare.Read);

static byte[] CorruptFinalStartXref(byte[] pdfBytes)
{
    var pdfText = Encoding.ASCII.GetString(pdfBytes);
    var startXrefIndex = pdfText.LastIndexOf("startxref", StringComparison.Ordinal);
    if (startXrefIndex < 0)
    {
        throw new InvalidOperationException("The source PDF did not contain a startxref marker.");
    }

    var valueStart = startXrefIndex + "startxref".Length;
    while (valueStart < pdfText.Length && char.IsWhiteSpace(pdfText[valueStart]))
    {
        valueStart++;
    }

    var valueEnd = valueStart;
    while (valueEnd < pdfText.Length && char.IsDigit(pdfText[valueEnd]))
    {
        valueEnd++;
    }

    if (valueEnd == valueStart)
    {
        throw new InvalidOperationException("The source PDF did not contain a numeric startxref value.");
    }

    var corruptedText = string.Concat(pdfText.AsSpan(0, valueStart), "999999999", pdfText.AsSpan(valueEnd));
    return Encoding.ASCII.GetBytes(corruptedText);
}

static string ResolveTestAssetPath(string relativePath)
{
    var candidate = IOPath.Combine(AppContext.BaseDirectory, relativePath);
    if (File.Exists(candidate))
    {
        return candidate;
    }

    var normalizedRelativePath = relativePath
        .Replace('/', IOPath.DirectorySeparatorChar)
        .Replace('\\', IOPath.DirectorySeparatorChar);

    var testFilesPrefix = $"testfiles{IOPath.DirectorySeparatorChar}";
    if (normalizedRelativePath.StartsWith(testFilesPrefix, StringComparison.OrdinalIgnoreCase))
    {
        normalizedRelativePath = normalizedRelativePath[testFilesPrefix.Length..];
    }

    return IOPath.GetFullPath(IOPath.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "tests",
        "TestFiles",
        normalizedRelativePath));
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
