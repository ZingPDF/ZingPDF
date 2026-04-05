using ZingPDF;

Directory.CreateDirectory("output");

await using var input = File.OpenRead(Path.Combine("testfiles", "pdf", "generated-mixed-workload.pdf"));
await using var output = File.Create(Path.Combine("output", "selected-pages.pdf"));
using var pdf = Pdf.Load(input);
using var selectedPages = await pdf.ExportPagesAsync([1, 3, 5]);

await selectedPages.SaveAsync(output);
