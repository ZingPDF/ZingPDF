using ZingPdf;
using ZingPdf.Core.Parsing;

await CreateNewPdfAndValidate();

//await ParseResaveValidate("test.pdf", "output.pdf");

static async Task ParseResaveValidate(string input, string output)
{
    var inputFileStream = new FileStream(input, FileMode.Open);

    var errors = ValidatePdf("Before", inputFileStream).ToList();
    inputFileStream.Position = 0;

    var pdf = await PdfParser.ParseAsync(inputFileStream);

    var outputFileStream = new FileStream(output, FileMode.Truncate);

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

static async Task CreateNewPdfAndValidate()
{
    var outputFileStream = new FileStream("output.pdf", FileMode.Truncate);
    var pdf = new Pdf();
    await pdf.WriteAsync(outputFileStream);

    outputFileStream.Position = 0;

    var errors = ValidatePdf("file", outputFileStream).ToList();
    foreach (var error in errors)
    {
        Console.WriteLine(error);
    }
}