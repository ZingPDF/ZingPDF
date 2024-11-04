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
using ZingPDF.Elements.Forms.FieldTypes;

//using var outputFileStream = new FileStream("output.pdf", FileMode.Create);
//var pdf = new Pdf();

//pdf.AppendPage();

//await pdf.WriteAsync(outputFileStream);

//await CreateNewPdfAndValidate("output.pdf");

//await ParseResaveValidate("Spec/ISO_32000-2-2020.pdf", "output.pdf");
//await ParseResaveValidate("Ghostscript.pdf", "output.pdf");
//await ParseResaveValidate("GS9_Color_Management.pdf", "output.pdf");
//await ParseResaveValidate("output.pdf", "output2.pdf");
//await ParseResaveValidate("form.pdf", "output.pdf");
//await ParseResaveValidate("test.pdf", "output.pdf");

//await ConvertFromHTML(new Uri("https://www.google.com"), "output.pdf");

//await AddPage("test.pdf", "output.pdf");

//await AddTextToPage();

//await AddImageToPage();

//await RotatePage();

//await RotateWholeDocument();

await CompleteForm("form.pdf", "output.pdf");

static async Task CompleteForm(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = await PdfParser.OpenAsync(inputFileStream);

    var form = pdf.GetForm()!;
    
    var fields = await form.GetFieldsAsync();

    foreach (var field in fields)
    {
        if (field is TextFormField textField)
        {
            textField.Value = "test";
        }
        else if (field is CheckboxFormField cbField)
        {
            cbField.Checkboxes[0].Check();
        }
    }

    await pdf.SaveAsync(outputFileStream);
}

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

static async Task RotateWholeDocument()
{
    using var inputFileStream = new FileStream("test.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = await PdfParser.OpenAsync(inputFileStream);

    await pdf.SetRotationAsync(Rotation.Degrees90);

    await pdf.SaveAsync(outputFileStream);
}

static async Task RotatePage()
{
    using var inputFileStream = new FileStream("test.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = await PdfParser.OpenAsync(inputFileStream);

    var page = await pdf.GetPageAsync(1);

    page.Rotate(Rotation.Degrees90);

    await pdf.SaveAsync(outputFileStream);
}

static async Task AddImageToPage()
{
    using var inputFileStream = new FileStream("test.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = await PdfParser.OpenAsync(inputFileStream);

    var page = await pdf.InsertPageAsync(1, new PageDictionary.PageCreationOptions { MediaBox = Rectangle.FromSize(200, 200) });

    await page.AddImageAsync(Image.FromFile("cat.jpg", Rectangle.FromSize(200, 200)));

    await pdf.SaveAsync(outputFileStream);
}

static async Task ConvertFromHTML(Uri uri, string output)
{
    using var outputFileStream = new FileStream(output, FileMode.Create);

    using var pdfStream = await Converter.ToPdfAsync(uri);

    await pdfStream.CopyToAsync(outputFileStream);
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

