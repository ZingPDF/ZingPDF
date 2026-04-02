using BenchmarkDotNet.Attributes;
using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Performance;

[MemoryDiagnoser]
public class PdfBenchmarks
{
    private int _mixedWorkloadLastPageNumber;
    private int _mixedWorkloadMiddlePageNumber;

    [GlobalSetup]
    public void Setup()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        _mixedWorkloadLastPageNumber = pdf.GetPageCountAsync().GetAwaiter().GetResult();
        _mixedWorkloadMiddlePageNumber = Math.Max(1, (_mixedWorkloadLastPageNumber + 1) / 2);
    }

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

    [Benchmark(Description = "Open and get the first page in a mixed-workload PDF")]
    public async Task GetFirstPage_MixedWorkloadPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        _ = await pdf.GetPageAsync(1);
    }

    [Benchmark(Description = "Open and get the middle page in a mixed-workload PDF")]
    public async Task GetMiddlePage_MixedWorkloadPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        _ = await pdf.GetPageAsync(_mixedWorkloadMiddlePageNumber);
    }

    [Benchmark(Description = "Open and get the last page in a mixed-workload PDF")]
    public async Task GetLastPage_MixedWorkloadPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        _ = await pdf.GetPageAsync(_mixedWorkloadLastPageNumber);
    }

    [Benchmark(Description = "Open, get the first page, and resolve MediaBox in a mixed-workload PDF")]
    public async Task GetFirstPageMediaBox_MixedWorkloadPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        var page = await pdf.GetPageAsync(1);
        _ = await page.Dictionary.MediaBox.GetAsync();
    }

    [Benchmark(Description = "Open, get the first page, and resolve Resources in a mixed-workload PDF")]
    public async Task GetFirstPageResources_MixedWorkloadPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        var page = await pdf.GetPageAsync(1);
        _ = await page.Dictionary.Resources.GetAsync();
    }

    [Benchmark(Description = "Extract text from a text-heavy PDF")]
    public async Task ExtractText_TextHeavyPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.TextHeavy));
        _ = await pdf.ExtractTextAsync();
    }

    [Benchmark(Description = "Open and extract text from the first page in a text-heavy PDF")]
    public async Task ExtractText_FirstPage_TextHeavyPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.TextHeavy));
        _ = await pdf.ExtractTextAsync(1);
    }

    [Benchmark(Description = "Append a page and save")]
    public async Task AppendPage_AndSave()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        using var output = new MemoryStream();

        _ = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(595, 842));
        await pdf.SaveAsync(output);
    }

    [Benchmark(Description = "Create a PDF and append 80 pages")]
    public async Task CreateAndAppend80Pages()
    {
        using var pdf = Pdf.Create();

        for (var i = 0; i < 80; i++)
        {
            _ = await pdf.AppendPageAsync();
        }
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
