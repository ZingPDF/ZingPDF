using System.Text;
using System.Text.RegularExpressions;
using WebSupergoo.ABCpdf12;
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

//XSettings.InstallLicense("X/VKS0cPn5FgsCJaaaGHZIP1K7JIQ4MYlq3wxL3FA0ojxkiVPH3rYMVWQ0lkwg8KCtYy4j5CuSEXr6IrQbB/xFEsfGKZBH4/3DFMO/XgBjbi1y7S5MlUFrjUWBKMcmImUL1oUMFb8wtwCFVZoTCQbGhYcSuWVW7qmqUR6D9AYuLEkpsjtDvZ9nfHqPN1nS8YTR8X9X1YxRzwMAM7U5B+zgFTpkGfF8Z/KMLeOGHkfuTbfV4bi8H8Pj4gmWjM");

//using var outputFileStream = new FileStream("output.pdf", FileMode.Create);
//var pdf = new Pdf();

//pdf.AppendPage();

//await pdf.WriteAsync(outputFileStream);

//await CreateNewPdfAndValidate("output.pdf");

//LoadAndSaveUsingAbcpdf("output.pdf", "output-abcpdf.pdf");

//await ParseResaveValidate("Spec/ISO_32000-2-2020.pdf", "output.pdf");
//await ParseResaveValidate("testfiles/pdf/Ghostscript.pdf", "output.pdf");
//await ParseResaveValidate("testfiles/pdf/GS9_Color_Management.pdf", "output.pdf");
//await ParseResaveValidate("output.pdf", "output2.pdf");
//await ParseResaveValidate("testfiles/pdf/form.pdf", "output.pdf");
//await ParseResaveValidate("testfiles/pdf/test.pdf", "output.pdf");

//await AppendPdf("testfiles/pdf/minimal3.pdf", "testfiles/pdf/minimal.pdf", "output.pdf");
//await AppendPdf("testfiles/pdf/minimal.pdf", "testfiles/pdf/form.pdf", "output.pdf");
//await AppendPdf("testfiles/pdf/test.pdf", "testfiles/pdf/form.pdf", "output.pdf");
//await AppendPdf("testfiles/pdf/combobox-form.pdf", "testfiles/pdf/test.pdf", "output.pdf");
//await AppendPdf("testfiles/pdf/minimal.pdf", "testfiles/pdf/minimal2.pdf", "output.pdf");
//LoadAndValidateUsingAbcpdf("output.pdf");
//LoadUsingAbcpdf("output.pdf");

//await Parse("testfiles/pdf/minimal.pdf");
//await Parse("testfiles/pdf/minimal3.pdf");
//await Parse("testfiles/pdf/test.pdf");
//await Parse("testfiles/pdf/form.pdf");
//await Parse("output.pdf");
//await Parse("testfiles/pdf/MikeyFlemingFreelance_Folio.pdf");
//await Parse("testfiles/pdf/encrypted.pdf");

//LoadAndValidateUsingAbcpdf("testfiles/pdf/test.pdf");
//LoadAndValidateUsingAbcpdf("testfiles/pdf/form.pdf");
//LoadAndValidateUsingAbcpdf("output.pdf");

//await ConvertFromHTML(new Uri("https://www.google.com"), "output.pdf");
//await ConvertFromHTMLContent("testfiles/html/form-test.html", "form-test.pdf");

//await AddPage("testfiles/pdf/test.pdf", "output.pdf");

//await AddTextToPage();

//await AddImageToPage();

//await RotatePage();

//await RotateWholeDocument();

//await CompleteForm("testfiles/pdf/complex-form.pdf", "output.pdf");
//LoadAndValidateUsingAbcpdf("testfiles/pdf/combobox-form.pdf");
//await CompleteForm("testfiles/pdf/combobox-form.pdf", "output.pdf");
//LoadAndValidateUsingAbcpdf("output.pdf");

//await WipeFields();

//await TempFieldApTest();

//await Test();

//await Decompress("testfiles/pdf/complex-form.pdf", "output.pdf");

await ExtractText("testfiles/pdf/complex-form.pdf");

static async Task ExtractText(string input)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    var pdf = await Pdf.LoadAsync(inputFileStream);

    await pdf.ExtractTextAsync();
}

static async Task Decompress(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = await Pdf.LoadAsync(inputFileStream);

    await pdf.DecompressAsync();

    await pdf.SaveAsync(outputFileStream);
}

static async Task WipeFields()
{
    using var inputFileStream = new FileStream("testfiles/pdf/combobox-form.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("blank-form.pdf", FileMode.Create);

    var pdf = await Pdf.LoadAsync(inputFileStream);

    var form = pdf.GetForm();

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

    var pdf = await Pdf.LoadAsync(inputFileStream);

    var form = pdf.GetForm();

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
    //using var inputFileStream = new FileStream("testfiles/pdf/MikeyFlemingFreelance_Folio.pdf", FileMode.Open);
    //using var inputFileStream = new FileStream("testfiles/pdf/complex-form.pdf", FileMode.Open);
    //using var inputFileStream = new FileStream("testfiles/pdf/combobox-form.pdf", FileMode.Open);
    using var inputFileStream = new FileStream("testfiles/pdf/encrypted.pdf", FileMode.Open);

    var pdf = await Pdf.LoadAsync(inputFileStream);

    //var encrypted = pdf.Encrypted;

    var count = await pdf.GetPageCountAsync();
}


static async Task Parse(string input)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);

    var pdf = await Pdf.LoadAsync(inputFileStream);

    var pageCount = await pdf.GetPageCountAsync();
}

static async Task AppendPdf(string input1, string input2, string output)
{
    using var inputFileStream1 = new FileStream(input1, FileMode.Open);
    using var inputFileStream2 = new FileStream(input2, FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = await Pdf.LoadAsync(inputFileStream1);

    await pdf.AppendPdfAsync(inputFileStream2);

    await pdf.SaveAsync(outputFileStream);
}

static async Task CompleteForm(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = await Pdf.LoadAsync(inputFileStream);

    var form = pdf.GetForm()!;
    
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

    var pdf = await Pdf.LoadAsync(inputFileStream);

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
    using var inputFileStream = new FileStream("test.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = await Pdf.LoadAsync(inputFileStream);

    await pdf.SetRotationAsync(Rotation.Degrees90);

    await pdf.SaveAsync(outputFileStream);
}

static async Task RotatePage()
{
    using var inputFileStream = new FileStream("testfiles/pdf/test.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = await Pdf.LoadAsync(inputFileStream);

    var page = await pdf.GetPageAsync(1);

    page.RotateAsync(Rotation.Degrees90);

    await pdf.SaveAsync(outputFileStream);
}

static async Task AddImageToPage()
{
    using var inputFileStream = new FileStream("testfiles/pdf/minimal.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = await Pdf.LoadAsync(inputFileStream);

    var page = await pdf.GetPageAsync(1);

    //var page = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromSize(200, 200));

    await page.AddImageAsync(Image.FromFile("testfiles/image/cat.jpg", Rectangle.FromDimensions(200, 200)));

    await pdf.SaveAsync(outputFileStream);
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

    var pdf = await Pdf.LoadAsync(inputFileStream);

    var count1 = await pdf.GetPageCountAsync();

    var page = await pdf.AppendPageAsync();

    var count2 = await pdf.GetPageCountAsync();

    await pdf.SaveAsync(outputFileStream);
}

static async Task ParseResaveValidate(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);

    var errors = ValidatePdf("Before", inputFileStream).ToList();
    inputFileStream.Position = 0;

    var pdf = await Pdf.LoadAsync(inputFileStream);

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

    //await pdf.GetPageAsync(1);

    outputFileStream.Position = 0;

    var errors2 = ValidatePdf("After", outputFileStream).ToList();

    var newErrors = errors2.Except(errors);
    var fixedErrors = errors.Except(errors2);

    Console.WriteLine("New errors:");
    foreach (var error in newErrors)
    {
       Console.WriteLine(error);
    }

    Console.WriteLine("Fixed errors:");
    foreach (var error in fixedErrors)
    {
        Console.WriteLine(error);
    }
}

static void LoadUsingAbcpdf(string inputPath)
{
    var doc = new Doc();
    doc.Read(inputPath);
}

static void LoadAndSaveUsingAbcpdf(string inputPath, string outputPath)
{
    var doc = new Doc();
    doc.Read(inputPath);

    doc.Save(outputPath);
}

static void LoadAndValidateUsingAbcpdf(string inputPath)
{
    using var inputFileStream = new FileStream(inputPath, FileMode.Open);

    var errors = ValidatePdf(inputPath, inputFileStream).ToList();

    foreach (var error in errors)
    {
        Console.WriteLine(error);
    }

    Console.WriteLine($"Total errors: {errors.Count}");
}

static IEnumerable<string> ValidatePdf(string name, FileStream fileStream)
{
    using var theOperation = new WebSupergoo.ABCpdf12.Operations.PdfValidationOperation();
    
    theOperation.Conformance = WebSupergoo.ABCpdf12.Operations.PdfConformance.PdfA3b;
    Doc doc = theOperation.Read(fileStream, options: null);
    doc.Dispose();

    foreach (var error in theOperation.Errors)
    {
        yield return $"Error: {error}";
    }

    foreach (var warning in theOperation.Warnings)
    {
        yield return $"Warning: {warning}";
    }

    Console.WriteLine($"Validated {name} with ABCpdf");
}

//static async Task CreateNewPdfAndValidate(string outputPath)
//{
//    using var outputFileStream = new FileStream(outputPath, FileMode.Create);
//    var pdf = Pdf.Create();

//    await pdf.AppendPageAsync();

//    await pdf.SaveAsync(outputFileStream);

//    outputFileStream.Position = 0;

//    var errors = ValidatePdf("file", outputFileStream).ToList();
//    foreach (var error in errors)
//    {
//        Console.WriteLine(error);
//    }
//}

