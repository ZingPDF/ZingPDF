using ZingPdf;
using ZingPdf.Core.Parsing;
using WebSupergoo.ABCpdf12;

XSettings.InstallLicense("X/VKS0cPn5FgsCJaaaGHZIP1K7JIQ4MYlq3wxL3FA0ojxkiVPH3rYMVWQ0lkwg8KCtYy4j5CuSEXr6IrQbB/xFEsfGKZBH4/3DFMO/XgBjbi1y7S5MlUFrjUWBKMcmImUL1oUMFb8wtwCFVZoTCQbGhYcSuWVW7qmqUR6D9AYuLEkpsjtDvZ9nfHqPN1nS8YTR8X9X1YxRzwMAM7U5B+zgFTpkGfF8Z/KMLeOGHkfuTbfV4bi8H8Pj4gmWjM");

//await CreateNewPdfAndValidate("output.pdf");

//LoadAndSaveUsingAbcpdf("output.pdf", "output-abcpdf.pdf");

await ParseResaveValidate("test2.pdf", "output.pdf");

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

static void LoadAndSaveUsingAbcpdf(string inputPath, string outputPath)
{
    var doc = new Doc();
    doc.Read(inputPath);

    doc.Save(outputPath);
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