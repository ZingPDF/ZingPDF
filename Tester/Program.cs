using ZingPdf;
using ZingPdf.Core.Parsing;

//var pdf = new Pdf();

//pdf.Pages.Add(new Page());

//await pdf.WriteAsync(new FileStream("output.pdf", FileMode.Truncate));

await new PdfParser().ParseAsync(new FileStream("test.pdf", FileMode.Open));