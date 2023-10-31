using ZingPdf;
using ZingPdf.Core.Parsing;

var input = "test2.pdf";
var output = "output.pdf";
var pdf = await PdfParser.ParseAsync(new FileStream(input, FileMode.Open));

var fileStream = new FileStream(output, FileMode.Truncate);
await pdf.WriteAsync(fileStream);

Console.WriteLine($"Parsed {input} to {output} with ZingPdf");
fileStream.Position = 0;

