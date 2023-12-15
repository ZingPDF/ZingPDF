using ZingPdf.Core.Parsing;
using ZingPdf.Core;
using System.Text.RegularExpressions;
using System.Text;

//using var outputFileStream = new FileStream("output.pdf", FileMode.Create);
//var pdf = new Pdf();

//pdf.AppendPage();

//await pdf.WriteAsync(outputFileStream);

//await CreateNewPdfAndValidate("output.pdf");

//await ParseResaveValidate("Spec/ISO_32000-2-2020.pdf", "output.pdf");
await ParseResaveValidate("Ghostscript.pdf", "output.pdf");
//await ParseResaveValidate("output.pdf", "output2.pdf");
//await ParseResaveValidate("test.pdf", "output.pdf");

//await ListObjNumbers("Spec/ISO_32000-2-2020.pdf");

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

static async Task ParseResaveValidate(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);

    //var errors = ValidatePdf("Before", inputFileStream).ToList();
    //inputFileStream.Position = 0;

    var pdf = Pdf.Load(inputFileStream);

    var count = await pdf.GetPageCountAsync();

    await pdf.AppendPageAsync();

    //var count2 = await pdf.GetPageCountAsync();

    //var test = await pdf.GetPageAsync(1);

    using var outputFileStream = new FileStream(output, FileMode.Create);

    await pdf.SaveAsync(outputFileStream);

    Console.WriteLine($"Parsed {input} to {output} with ZingPdf");

    //await pdf.GetPageAsync(1);

    //outputFileStream.Position = 0;

    //var errors2 = ValidatePdf("After", outputFileStream).ToList();

    //var newErrors = errors2.Except(errors);

    //foreach (var error in newErrors)
    //{
    //    Console.WriteLine(error);
    //}
}

static async Task CreateNewPdfAndValidate(string outputPath)
{
    using var outputFileStream = new FileStream(outputPath, FileMode.Create);
    var pdf = Pdf.Create();

    await pdf.AppendPageAsync();

    await pdf.SaveAsync(outputFileStream);

    outputFileStream.Position = 0;

    var errors = ValidatePdf("file", outputFileStream).ToList();
    foreach (var error in errors)
    {
        Console.WriteLine(error);
    }
}