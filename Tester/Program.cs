using ZingPdf;

var pdf = new Pdf();

pdf.Pages.Add(new Page());

await pdf.WriteAsync(new FileStream("output.pdf", FileMode.Truncate));