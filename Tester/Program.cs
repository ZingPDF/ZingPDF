using ZingPdf;
using ZingPdf.Core.Parsing;

//var pdf = new Pdf();

//pdf.Pages.Add(new Page());

var pdf = await PdfParser.ParseAsync(new FileStream("test.pdf", FileMode.Open));

await pdf.WriteAsync(new FileStream("output.pdf", FileMode.Truncate));

Console.WriteLine("Done parsing");