using BenchmarkDotNet.Attributes;
using ZingPDF.Rendering;

namespace ZingPDF.Performance;

[System.Runtime.Versioning.SupportedOSPlatform("android31.0")]
[System.Runtime.Versioning.SupportedOSPlatform("ios13.6")]
[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[System.Runtime.Versioning.SupportedOSPlatform("maccatalyst13.5")]
[System.Runtime.Versioning.SupportedOSPlatform("macos")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
[MemoryDiagnoser]
public class PageRenderBenchmarks
{
    [Benchmark(Description = "ZingPDF: Render first page PNG preview at 1x")]
    public async Task RenderFirstPage_OneX_MixedWorkloadPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        _ = await pdf.RenderPageAsync(1, new PdfPageRenderOptions { Scale = 1d });
    }

    [Benchmark(Description = "ZingPDF: Render first page PNG thumbnail at 0.25x")]
    public async Task RenderFirstPage_Thumbnail_MixedWorkloadPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        _ = await pdf.RenderPageAsync(1, new PdfPageRenderOptions { Scale = 0.25d });
    }

    [Benchmark(Description = "ZingPDF: Render first page PNG preview from image-heavy PDF at 1x")]
    public async Task RenderFirstPage_OneX_ImageHeavyPdf()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.ImageHeavy));
        _ = await pdf.RenderPageAsync(1, new PdfPageRenderOptions { Scale = 1d });
    }

    [Benchmark(Description = "ZingPDF: Render edited unsaved page PNG preview at 1x")]
    public async Task RenderEditedPage_OneX_BeforeSave()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        var page = await pdf.GetPageAsync(1);
        await page.AddWatermarkAsync("PREVIEW");

        _ = await page.RenderAsync(new PdfPageRenderOptions { Scale = 1d });
    }
}
