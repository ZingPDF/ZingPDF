using ZingPdf;
using ZingPdf.Core.Parsing;

var input = "test2.pdf";
var output = "output.pdf";
var pdf = await PdfParser.ParseAsync(new FileStream(input, FileMode.Open));

var fileStream = new FileStream(output, FileMode.Truncate);
await pdf.WriteAsync(fileStream);

Console.WriteLine($"Parsed {input} to {output} with ZingPdf");
fileStream.Position = 0;

WebSupergoo.ABCpdf12.XSettings.InstallLicense("X/VKS0cPn5FgsCJaaaGHZIP1K7JIQ4MYlq3wxL3FA0ojxkiVPH3rYMVWQ0lkwg8KCtYy4j5CuSEXr6IrQbB/xFEsfGKZBH4/3DFMO/XgBjbi1y7S5MlUFrjUWBKMcmImUL1oUMFb8wtwCFVZoTCQbGhYcSuWVW7qmqUR6D9AYuLEkpsjtDvZ9nfHqPN1nS8YTR8X9X1YxRzwMAM7U5B+zgFTpkGfF8Z/KMLeOGHkfuTbfV4bi8H8Pj4gmWjM");

var document = new WebSupergoo.ABCpdf12.Doc();
document.Read(fileStream);//, new WebSupergoo.ABCpdf12.XReadOptions { ErrorHandling = WebSupergoo.ABCpdf12.ErrorHandlingType.OutputUntilError });

Console.WriteLine($"Parsed {output} with ABCpdf");