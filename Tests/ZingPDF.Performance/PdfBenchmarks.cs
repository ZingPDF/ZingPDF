using BenchmarkDotNet.Attributes;
using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Performance;

[MemoryDiagnoser]
public class PdfBenchmarks
{
    [Benchmark(Description = "Open a minimal PDF")]
    public void Open_MinimalPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
    }

    [Benchmark(Description = "Open and count pages in a minimal PDF")]
    public async Task CountPages_MinimalPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        _ = await pdf.GetPageCountAsync();
    }

    [Benchmark(Description = "Open a minimal PDF and parse versions")]
    public async Task OpenAndParseVersions_MinimalPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        _ = await pdf.Objects.GetLatestTrailerDictionaryAsync();
    }

    [Benchmark(Description = "Open a minimal PDF, parse versions, and read the catalog reference")]
    public async Task OpenParseVersionsAndReadCatalogReference_MinimalPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        var trailer = await pdf.Objects.GetLatestTrailerDictionaryAsync();
        _ = trailer.Root;
    }

    [Benchmark(Description = "Open a minimal PDF, parse versions, and dereference the catalog object")]
    public async Task OpenParseVersionsAndDereferenceCatalogObject_MinimalPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        var trailer = await pdf.Objects.GetLatestTrailerDictionaryAsync();
        _ = await pdf.Objects.GetAsync(trailer.Root!);
    }

    [Benchmark(Description = "Open a minimal PDF and resolve the document catalog")]
    public async Task OpenAndResolveCatalog_MinimalPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        _ = await pdf.Objects.GetDocumentCatalogAsync();
    }

    [Benchmark(Description = "Open a minimal PDF and resolve the root page tree")]
    public async Task OpenAndResolveRootPageTree_MinimalPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        _ = await pdf.Objects.PageTree.GetRootPageTreeNodeAsync();
    }

    [Benchmark(Description = "Open a larger real-world PDF")]
    public void Open_RealWorldPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.ImageHeavy));
    }

    [Benchmark(Description = "Open and count pages in a larger real-world PDF")]
    public async Task CountPages_RealWorldPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.ImageHeavy));
        _ = await pdf.GetPageCountAsync();
    }

    [Benchmark(Description = "Extract text from a text-heavy PDF")]
    public async Task ExtractText_TextHeavyPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.TextHeavy));
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
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.ImageHeavy));
        using var output = new MemoryStream();

        pdf.Compress(144, 75);
        await pdf.SaveAsync(output);
    }
}
