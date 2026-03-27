using BenchmarkDotNet.Attributes;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using UglyToad.PdfPig;

namespace ZingPDF.Performance;

[MemoryDiagnoser]
public class CompetitiveBenchmarks
{
    [Benchmark(Description = "ZingPDF: Open a minimal PDF")]
    public void ZingPdf_Open_MinimalPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
    }

    [Benchmark(Description = "PDFsharp: Open a minimal PDF")]
    public void PdfSharp_Open_MinimalPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.Minimal);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
    }

    [Benchmark(Description = "PdfPig: Open a minimal PDF")]
    public void PdfPig_Open_MinimalPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.Minimal);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
    }

    [Benchmark(Description = "ZingPDF: Open and count pages in a minimal PDF")]
    public async Task ZingPdf_CountPages_MinimalPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        _ = await pdf.GetPageCountAsync();
    }

    [Benchmark(Description = "PDFsharp: Open and count pages in a minimal PDF")]
    public void PdfSharp_CountPages_MinimalPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.Minimal);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        _ = pdf.PageCount;
    }

    [Benchmark(Description = "PdfPig: Open and count pages in a minimal PDF")]
    public void PdfPig_CountPages_MinimalPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.Minimal);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
        _ = pdf.NumberOfPages;
    }

    [Benchmark(Description = "ZingPDF: Open a larger real-world PDF")]
    public void ZingPdf_Open_RealWorldPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.ImageHeavy));
    }

    [Benchmark(Description = "PDFsharp: Open a larger real-world PDF")]
    public void PdfSharp_Open_RealWorldPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.ImageHeavy);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
    }

    [Benchmark(Description = "PdfPig: Open a larger real-world PDF")]
    public void PdfPig_Open_RealWorldPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.ImageHeavy);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
    }

    [Benchmark(Description = "ZingPDF: Open and count pages in a larger real-world PDF")]
    public async Task ZingPdf_CountPages_RealWorldPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.ImageHeavy));
        _ = await pdf.GetPageCountAsync();
    }

    [Benchmark(Description = "PDFsharp: Open and count pages in a larger real-world PDF")]
    public void PdfSharp_CountPages_RealWorldPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.ImageHeavy);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        _ = pdf.PageCount;
    }

    [Benchmark(Description = "PdfPig: Open and count pages in a larger real-world PDF")]
    public void PdfPig_CountPages_RealWorldPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.ImageHeavy);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
        _ = pdf.NumberOfPages;
    }

    [Benchmark(Description = "ZingPDF: Extract text from a text-heavy PDF")]
    public async Task ZingPdf_ExtractText_TextHeavyPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.TextHeavy));
        _ = await pdf.ExtractTextAsync();
    }

    [Benchmark(Description = "PdfPig: Extract text from a text-heavy PDF")]
    public void PdfPig_ExtractText_TextHeavyPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.TextHeavy);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);

        foreach (var page in pdf.GetPages())
        {
            _ = page.Text;
        }
    }

    [Benchmark(Description = "ZingPDF: Append a page and save")]
    public async Task ZingPdf_AppendPage_AndSave()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        using var output = new MemoryStream();

        _ = await pdf.AppendPageAsync(options => options.MediaBox = ZingPDF.Syntax.CommonDataStructures.Rectangle.FromDimensions(595, 842));
        await pdf.SaveAsync(output);
    }

    [Benchmark(Description = "PDFsharp: Append a page and save")]
    public void PdfSharp_AppendPage_AndSave()
    {
        using var stream = TestFiles.OpenStream(TestFiles.Minimal);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Modify);
        using var output = new MemoryStream();

        pdf.AddPage(new PdfPage());
        pdf.Save(output, false);
    }

}
