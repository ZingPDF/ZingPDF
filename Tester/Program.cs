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

//await ListObjNumbers("Spec/ISO_32000-2-2020.pdf");

await ConvertFromHTML(new Uri("https://www.google.com"), "output.pdf");

static async Task ConvertFromHTML(Uri uri, string output)
{
    using var outputFileStream = new FileStream(output, FileMode.Create);

    using var pdfStream = await Converter.ToPdfAsync(uri);

    await pdfStream.CopyToAsync(outputFileStream);
}

static async Task ParseResaveValidate(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);

    var errors = ValidatePdf("Before", inputFileStream).ToList();
    inputFileStream.Position = 0;

    var pdf = await PdfParser.OpenAsync(inputFileStream);

    var test = await pdf.IndirectObjects.GetAsync(new IndirectObjectReference(new IndirectObjectId(17, 0)));

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

static async Task ListObjNumbers(string input)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);

    using var reader = new StreamReader(inputFileStream);

    string content = reader.ReadToEnd();
    MatchCollection matches = Regex.Matches(content, @"([\d]+) [\d]+ obj");

    var csvBuilder = new StringBuilder();

    foreach (var match in matches.Cast<Match>())
    {
        csvBuilder.AppendLine(match.Groups[1].Value);
    }

    var csvDetailFilename = $"output.csv";
    File.WriteAllText(csvDetailFilename, csvBuilder.ToString());
    Console.WriteLine($"Saved as {csvDetailFilename}");

}
