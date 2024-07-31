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

XSettings.InstallLicense("X/VKS0cPn5FgsCJaaaGHZIP1K7JIQ4MYlq3wxL3FA0ojxkiVPH3rYMVWQ0lkwg8KCtYy4j5CuSEXr6IrQbB/xFEsfGKZBH4/3DFMO/XgBjbi1y7S5MlUFrjUWBKMcmImUL1oUMFb8wtwCFVZoTCQbGhYcSuWVW7qmqUR6D9AYuLEkpsjtDvZ9nfHqPN1nS8YTR8X9X1YxRzwMAM7U5B+zgFTpkGfF8Z/KMLeOGHkfuTbfV4bi8H8Pj4gmWjM");

//using var outputFileStream = new FileStream("output.pdf", FileMode.Create);
//var pdf = new Pdf();

//pdf.AppendPage();

//await pdf.WriteAsync(outputFileStream);

//await CreateNewPdfAndValidate("output.pdf");

//LoadAndSaveUsingAbcpdf("output.pdf", "output-abcpdf.pdf");

//await ParseResaveValidate("Spec/ISO_32000-2-2020.pdf", "output.pdf");
//await ParseResaveValidate("Ghostscript.pdf", "output.pdf");
//await ParseResaveValidate("GS9_Color_Management.pdf", "output.pdf");
//await ParseResaveValidate("output.pdf", "output2.pdf");
//await ParseResaveValidate("form.pdf", "output.pdf");
//await ParseResaveValidate("test.pdf", "output.pdf");

//LoadAndValidateUsingAbcpdf("Ghostscript.pdf");
//LoadAndValidateUsingAbcpdf("output.pdf");

//await ConvertFromHTML(new Uri("https://www.google.com"), "output.pdf");

//await AddPage("test.pdf", "output.pdf");

await AddTextToPage();

static async Task AddTextToPage()
{
    using var inputFileStream = new FileStream("test.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = await PdfParser.OpenAsync(inputFileStream);

    var page = await pdf.InsertPageAsync(1, new PageDictionary.PageCreationOptions { MediaBox = Rectangle.FromSize(200, 200) });

    page.AddText(new ZingPDF.Text.TextObject(
        "test",
        new Rectangle(new Coordinate(10, 10), new Coordinate(100, 100)),
        new Coordinate(10, 50),
        new ZingPDF.Text.TextObject.FontOptions("Helv", 24, RGBColour.PrimaryRed)
        ));

    await pdf.SaveAsync(outputFileStream);
}

static async Task ConvertFromHTML(Uri uri, string output)
{
    using var outputFileStream = new FileStream(output, FileMode.Create);

    using var pdfStream = await Converter.ToPdfAsync(uri);

    await pdfStream.CopyToAsync(outputFileStream);
}

static async Task CompleteForm(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = await PdfParser.OpenAsync(inputFileStream);

    var fields = await pdf.GetFieldsAsync();

    await pdf.CompleteFormAsync(fields.ToDictionary(f => f.Name, f => "TEST"));

    await pdf.SaveAsync(outputFileStream);
}

static async Task AddPage(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = await PdfParser.OpenAsync(inputFileStream);

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

    var pdf = await PdfParser.OpenAsync(inputFileStream);

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

    var fields = await pdf.GetFieldsAsync();

    await pdf.CompleteFormAsync(fields.ToDictionary(f => f.Name, f => "TEST"));

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

