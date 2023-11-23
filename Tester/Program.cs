using ZingPdf.Core.Parsing;
using ZingPdf.Core;

//using var outputFileStream = new FileStream("output.pdf", FileMode.Create);
//var pdf = new Pdf();

//pdf.AppendPage();

//await pdf.WriteAsync(outputFileStream);

//await CreateNewPdfAndValidate("output.pdf");

await ParseResaveValidate("Spec/ISO_32000-2-2020.pdf", "output.pdf");

static async Task ParseResaveValidate(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);

    //var errors = ValidatePdf("Before", inputFileStream).ToList();
    //inputFileStream.Position = 0;

    var pdf = await Pdf.LoadAsync(inputFileStream);

    var count = await pdf.GetPageCountAsync();

    //await pdf.AppendPageAsync();

    //var count2 = await pdf.GetPageCountAsync();

    //var test = await pdf.GetPageAsync(1);

    using var outputFileStream = new FileStream(output, FileMode.Create);

    await pdf.SaveAsync(outputFileStream);

    Console.WriteLine($"Parsed {input} to {output} with ZingPdf");

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