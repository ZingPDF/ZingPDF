using BenchmarkDotNet.Attributes;
using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Performance;

[MemoryDiagnoser]
public class PdfBenchmarks
{
    [Benchmark(Description = "Open and count pages in a minimal PDF")]
    public async Task CountPages_MinimalPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        _ = await pdf.GetPageCountAsync();
    }

    [Benchmark(Description = "Open and count pages in a larger real-world PDF")]
    public async Task CountPages_RealWorldPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Ghostscript));
        _ = await pdf.GetPageCountAsync();
    }

    [Benchmark(Description = "Extract text from a portfolio PDF")]
    public async Task ExtractText_PortfolioPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Portfolio));
        _ = await pdf.ExtractTextAsync();
    }

    [Benchmark(Description = "Append a page and save")]
    public async Task AppendPage_AndSave()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        using var output = new MemoryStream();

        _ = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(595, 842));
        await pdf.SaveAsync(output);
    }

    [Benchmark(Description = "Add a watermark and save")]
    public async Task AddWatermark_AndSave()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        using var output = new MemoryStream();

        await pdf.AddWatermarkAsync("FAST");
        await pdf.SaveAsync(output);
    }

    [Benchmark(Description = "Compress a larger real-world PDF and save")]
    public async Task Compress_AndSave_RealWorldPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Ghostscript));
        using var output = new MemoryStream();

        pdf.Compress(144, 75);
        await pdf.SaveAsync(output);
    }
}
