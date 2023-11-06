using ZingPdf;
using ZingPdf.Core.Parsing;

//await CreateNewPdfAndValidate("output.pdf");

await ParseResaveValidate("test.pdf", "output.pdf");

static async Task ParseResaveValidate(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);

    var errors = ValidatePdf("Before", inputFileStream).ToList();
    inputFileStream.Position = 0;

    var pdf = await PdfParser.ParseAsync(inputFileStream);

    using var outputFileStream = new FileStream(output, FileMode.Create);

    await pdf.WriteAsync(outputFileStream);

    Console.WriteLine($"Parsed {input} to {output} with ZingPdf");
    outputFileStream.Position = 0;

    var errors2 = ValidatePdf("After", outputFileStream).ToList();

    var newErrors = errors.Except(errors2);

    foreach (var error in newErrors)
    {
        Console.WriteLine(error);
    }
}

static async Task CreateNewPdfAndValidate(string outputPath)
{
    using var outputFileStream = new FileStream(outputPath, FileMode.Truncate);
    var pdf = new Pdf();
    await pdf.WriteAsync(outputFileStream);

    outputFileStream.Position = 0;

    var errors = ValidatePdf("file", outputFileStream).ToList();
    foreach (var error in errors)
    {
        Console.WriteLine(error);
    }
}