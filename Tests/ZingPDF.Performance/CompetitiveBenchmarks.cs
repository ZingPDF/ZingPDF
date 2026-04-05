using BenchmarkDotNet.Attributes;
using iText.Kernel.Pdf.Canvas.Parser;
using ITextPageSize = iText.Kernel.Geom.PageSize;
using ITextPdfDocument = iText.Kernel.Pdf.PdfDocument;
using ITextPdfReader = iText.Kernel.Pdf.PdfReader;
using ITextPdfWriter = iText.Kernel.Pdf.PdfWriter;
using ITextPdfMerger = iText.Kernel.Utils.PdfMerger;
using PdfDocumentOpenMode = PdfSharp.Pdf.IO.PdfDocumentOpenMode;
using PdfPage = PdfSharp.Pdf.PdfPage;
using PdfReader = PdfSharp.Pdf.IO.PdfReader;
using Rectangle = ZingPDF.Syntax.CommonDataStructures.Rectangle;
using UglyToad.PdfPig;
using ZingPDF.Elements.Drawing.Text.Extraction;

namespace ZingPDF.Performance;

[MemoryDiagnoser]
public class CompetitiveBenchmarks
{
    private int _mixedWorkloadLastPageNumber;
    private int _mixedWorkloadMiddlePageNumber;
    private int _mixedWorkloadLastPageIndex;
    private int _mixedWorkloadMiddlePageIndex;
    private ZingPDF.Pdf? _openedZingTestPdf;
    private UglyToad.PdfPig.PdfDocument? _openedPdfPigTestPdf;
    private ZingPDF.Pdf? _openedZingTextHeavyPdf;
    private UglyToad.PdfPig.PdfDocument? _openedPdfPigTextHeavyPdf;

    [GlobalSetup]
    public void Setup()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        _mixedWorkloadLastPageNumber = pdf.GetPageCountAsync().GetAwaiter().GetResult();
        _mixedWorkloadMiddlePageNumber = Math.Max(1, (_mixedWorkloadLastPageNumber + 1) / 2);
        _mixedWorkloadLastPageIndex = _mixedWorkloadLastPageNumber - 1;
        _mixedWorkloadMiddlePageIndex = _mixedWorkloadMiddlePageNumber - 1;
    }

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

    [Benchmark(Description = "iText: Open a minimal PDF")]
    public void IText_Open_MinimalPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.Minimal);
        using var reader = new ITextPdfReader(stream);
        using var pdf = new ITextPdfDocument(reader);
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

    [Benchmark(Description = "iText: Open and count pages in a minimal PDF")]
    public void IText_CountPages_MinimalPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.Minimal);
        using var reader = new ITextPdfReader(stream);
        using var pdf = new ITextPdfDocument(reader);
        _ = pdf.GetNumberOfPages();
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

    [Benchmark(Description = "iText: Open a larger real-world PDF")]
    public void IText_Open_RealWorldPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.ImageHeavy);
        using var reader = new ITextPdfReader(stream);
        using var pdf = new ITextPdfDocument(reader);
    }

    [Benchmark(Description = "ZingPDF: Open and count pages in a larger real-world PDF")]
    public async Task ZingPdf_CountPages_RealWorldPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.ImageHeavy));
        _ = await pdf.GetPageCountAsync();
    }

    [Benchmark(Description = "ZingPDF: Open and get the first page in a mixed-workload PDF")]
    public async Task ZingPdf_GetFirstPage_MixedWorkloadPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        _ = await pdf.GetPageAsync(1);
    }

    [Benchmark(Description = "PDFsharp: Open and get the first page in a mixed-workload PDF")]
    public void PdfSharp_GetFirstPage_MixedWorkloadPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        _ = pdf.Pages[0];
    }

    [Benchmark(Description = "PdfPig: Open and get the first page in a mixed-workload PDF")]
    public void PdfPig_GetFirstPage_MixedWorkloadPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
        _ = pdf.GetPage(1);
    }

    [Benchmark(Description = "iText: Open and get the first page in a mixed-workload PDF")]
    public void IText_GetFirstPage_MixedWorkloadPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var reader = new ITextPdfReader(stream);
        using var pdf = new ITextPdfDocument(reader);
        _ = pdf.GetPage(1);
    }

    [Benchmark(Description = "ZingPDF: Open and get the middle page in a mixed-workload PDF")]
    public async Task ZingPdf_GetMiddlePage_MixedWorkloadPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        _ = await pdf.GetPageAsync(_mixedWorkloadMiddlePageNumber);
    }

    [Benchmark(Description = "PDFsharp: Open and get the middle page in a mixed-workload PDF")]
    public void PdfSharp_GetMiddlePage_MixedWorkloadPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        _ = pdf.Pages[_mixedWorkloadMiddlePageIndex];
    }

    [Benchmark(Description = "PdfPig: Open and get the middle page in a mixed-workload PDF")]
    public void PdfPig_GetMiddlePage_MixedWorkloadPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
        _ = pdf.GetPage(_mixedWorkloadMiddlePageNumber);
    }

    [Benchmark(Description = "iText: Open and get the middle page in a mixed-workload PDF")]
    public void IText_GetMiddlePage_MixedWorkloadPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var reader = new ITextPdfReader(stream);
        using var pdf = new ITextPdfDocument(reader);
        _ = pdf.GetPage(_mixedWorkloadMiddlePageNumber);
    }

    [Benchmark(Description = "ZingPDF: Open and get the last page in a mixed-workload PDF")]
    public async Task ZingPdf_GetLastPage_MixedWorkloadPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        _ = await pdf.GetPageAsync(_mixedWorkloadLastPageNumber);
    }

    [Benchmark(Description = "PDFsharp: Open and get the last page in a mixed-workload PDF")]
    public void PdfSharp_GetLastPage_MixedWorkloadPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        _ = pdf.Pages[_mixedWorkloadLastPageIndex];
    }

    [Benchmark(Description = "PdfPig: Open and get the last page in a mixed-workload PDF")]
    public void PdfPig_GetLastPage_MixedWorkloadPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
        _ = pdf.GetPage(_mixedWorkloadLastPageNumber);
    }

    [Benchmark(Description = "iText: Open and get the last page in a mixed-workload PDF")]
    public void IText_GetLastPage_MixedWorkloadPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var reader = new ITextPdfReader(stream);
        using var pdf = new ITextPdfDocument(reader);
        _ = pdf.GetPage(_mixedWorkloadLastPageNumber);
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

    [Benchmark(Description = "iText: Open and count pages in a larger real-world PDF")]
    public void IText_CountPages_RealWorldPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.ImageHeavy);
        using var reader = new ITextPdfReader(stream);
        using var pdf = new ITextPdfDocument(reader);
        _ = pdf.GetNumberOfPages();
    }

    [Benchmark(Description = "ZingPDF: Extract plain text from a text-heavy PDF")]
    public async Task ZingPdf_ExtractText_TextHeavyPdf()
    {
        ResolvedFontResourceSet.ClearGlobalCache();
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.TextHeavy));
        _ = (await pdf.ExtractTextAsync(new TextExtractionOptions { OutputKind = TextExtractionOutputKind.PlainText })).PlainText;
    }

    [Benchmark(Description = "ZingPDF: Open and extract plain text from the first page in a text-heavy PDF")]
    public async Task ZingPdf_ExtractText_FirstPage_TextHeavyPdf()
    {
        ResolvedFontResourceSet.ClearGlobalCache();
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.TextHeavy));
        _ = (await pdf.ExtractTextAsync(1, new TextExtractionOptions { OutputKind = TextExtractionOutputKind.PlainText })).PlainText;
    }

    [Benchmark(Description = "PdfPig: Extract plain text from a text-heavy PDF")]
    public void PdfPig_ExtractText_TextHeavyPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.TextHeavy);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);

        foreach (var page in pdf.GetPages())
        {
            _ = page.Text;
        }
    }

    [Benchmark(Description = "iText: Extract plain text from a text-heavy PDF")]
    public void IText_ExtractText_TextHeavyPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.TextHeavy);
        using var reader = new ITextPdfReader(stream);
        using var pdf = new ITextPdfDocument(reader);

        for (var pageNumber = 1; pageNumber <= pdf.GetNumberOfPages(); pageNumber++)
        {
            _ = PdfTextExtractor.GetTextFromPage(pdf.GetPage(pageNumber));
        }
    }

    [Benchmark(Description = "PdfPig: Open and extract plain text from the first page in a text-heavy PDF")]
    public void PdfPig_ExtractText_FirstPage_TextHeavyPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.TextHeavy);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
        _ = pdf.GetPage(1).Text;
    }

    [Benchmark(Description = "iText: Open and extract plain text from the first page in a text-heavy PDF")]
    public void IText_ExtractText_FirstPage_TextHeavyPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.TextHeavy);
        using var reader = new ITextPdfReader(stream);
        using var pdf = new ITextPdfDocument(reader);
        _ = PdfTextExtractor.GetTextFromPage(pdf.GetPage(1));
    }

    [Benchmark(Description = "ZingPDF: Open and extract plain text from a small composite-font PDF")]
    public async Task ZingPdf_ExtractText_TestPdf()
    {
        ResolvedFontResourceSet.ClearGlobalCache();
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.Test));
        _ = (await pdf.ExtractTextAsync(new TextExtractionOptions { OutputKind = TextExtractionOutputKind.PlainText })).PlainText;
    }

    [Benchmark(Description = "PdfPig: Open and extract plain text from a small composite-font PDF")]
    public void PdfPig_ExtractText_TestPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.Test);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);

        foreach (var page in pdf.GetPages())
        {
            _ = page.Text;
        }
    }

    [Benchmark(Description = "iText: Open and extract plain text from a small composite-font PDF")]
    public void IText_ExtractText_TestPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.Test);
        using var reader = new ITextPdfReader(stream);
        using var pdf = new ITextPdfDocument(reader);

        for (var pageNumber = 1; pageNumber <= pdf.GetNumberOfPages(); pageNumber++)
        {
            _ = PdfTextExtractor.GetTextFromPage(pdf.GetPage(pageNumber));
        }
    }

    [Benchmark(Description = "ZingPDF: Open and extract plain text from the first page in a small composite-font PDF")]
    public async Task ZingPdf_ExtractText_FirstPage_TestPdf()
    {
        ResolvedFontResourceSet.ClearGlobalCache();
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.Test));
        _ = (await pdf.ExtractTextAsync(1, new TextExtractionOptions { OutputKind = TextExtractionOutputKind.PlainText })).PlainText;
    }

    [Benchmark(Description = "PdfPig: Open and extract plain text from the first page in a small composite-font PDF")]
    public void PdfPig_ExtractText_FirstPage_TestPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.Test);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
        _ = pdf.GetPage(1).Text;
    }

    [Benchmark(Description = "iText: Open and extract plain text from the first page in a small composite-font PDF")]
    public void IText_ExtractText_FirstPage_TestPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.Test);
        using var reader = new ITextPdfReader(stream);
        using var pdf = new ITextPdfDocument(reader);
        _ = PdfTextExtractor.GetTextFromPage(pdf.GetPage(1));
    }

    [IterationSetup(Target = nameof(ZingPdf_ExtractText_FirstPage_TestPdf_Opened))]
    public void SetupZingPdf_ExtractText_FirstPage_TestPdf_Opened()
    {
        ResolvedFontResourceSet.ClearGlobalCache();
        _openedZingTestPdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.Test));
    }

    [IterationCleanup(Target = nameof(ZingPdf_ExtractText_FirstPage_TestPdf_Opened))]
    public void CleanupZingPdf_ExtractText_FirstPage_TestPdf_Opened()
    {
        _openedZingTestPdf?.Dispose();
        _openedZingTestPdf = null;
    }

    [Benchmark(Description = "ZingPDF: Extract plain text from the first page in an already-open small composite-font PDF")]
    public async Task ZingPdf_ExtractText_FirstPage_TestPdf_Opened()
    {
        ResolvedFontResourceSet.ClearGlobalCache();
        _ = (await _openedZingTestPdf!.ExtractTextAsync(1, new TextExtractionOptions { OutputKind = TextExtractionOutputKind.PlainText })).PlainText;
    }

    [IterationSetup(Target = nameof(PdfPig_ExtractText_FirstPage_TestPdf_Opened))]
    public void SetupPdfPig_ExtractText_FirstPage_TestPdf_Opened()
    {
        _openedPdfPigTestPdf = UglyToad.PdfPig.PdfDocument.Open(TestFiles.OpenStream(TestFiles.Test));
    }

    [IterationCleanup(Target = nameof(PdfPig_ExtractText_FirstPage_TestPdf_Opened))]
    public void CleanupPdfPig_ExtractText_FirstPage_TestPdf_Opened()
    {
        _openedPdfPigTestPdf?.Dispose();
        _openedPdfPigTestPdf = null;
    }

    [Benchmark(Description = "PdfPig: Extract plain text from the first page in an already-open small composite-font PDF")]
    public void PdfPig_ExtractText_FirstPage_TestPdf_Opened()
    {
        _ = _openedPdfPigTestPdf!.GetPage(1).Text;
    }

    [IterationSetup(Target = nameof(ZingPdf_ExtractText_FirstPage_TextHeavyPdf_Opened))]
    public void SetupZingPdf_ExtractText_FirstPage_TextHeavyPdf_Opened()
    {
        ResolvedFontResourceSet.ClearGlobalCache();
        _openedZingTextHeavyPdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.TextHeavy));
    }

    [IterationCleanup(Target = nameof(ZingPdf_ExtractText_FirstPage_TextHeavyPdf_Opened))]
    public void CleanupZingPdf_ExtractText_FirstPage_TextHeavyPdf_Opened()
    {
        _openedZingTextHeavyPdf?.Dispose();
        _openedZingTextHeavyPdf = null;
    }

    [Benchmark(Description = "ZingPDF: Extract plain text from the first page in an already-open text-heavy PDF")]
    public async Task ZingPdf_ExtractText_FirstPage_TextHeavyPdf_Opened()
    {
        ResolvedFontResourceSet.ClearGlobalCache();
        _ = (await _openedZingTextHeavyPdf!.ExtractTextAsync(1, new TextExtractionOptions { OutputKind = TextExtractionOutputKind.PlainText })).PlainText;
    }

    [IterationSetup(Target = nameof(PdfPig_ExtractText_FirstPage_TextHeavyPdf_Opened))]
    public void SetupPdfPig_ExtractText_FirstPage_TextHeavyPdf_Opened()
    {
        _openedPdfPigTextHeavyPdf = UglyToad.PdfPig.PdfDocument.Open(TestFiles.OpenStream(TestFiles.TextHeavy));
    }

    [IterationCleanup(Target = nameof(PdfPig_ExtractText_FirstPage_TextHeavyPdf_Opened))]
    public void CleanupPdfPig_ExtractText_FirstPage_TextHeavyPdf_Opened()
    {
        _openedPdfPigTextHeavyPdf?.Dispose();
        _openedPdfPigTextHeavyPdf = null;
    }

    [Benchmark(Description = "PdfPig: Extract plain text from the first page in an already-open text-heavy PDF")]
    public void PdfPig_ExtractText_FirstPage_TextHeavyPdf_Opened()
    {
        _ = _openedPdfPigTextHeavyPdf!.GetPage(1).Text;
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

    [Benchmark(Description = "iText: Append a page and save")]
    public void IText_AppendPage_AndSave()
    {
        using var input = TestFiles.OpenStream(TestFiles.Minimal);
        using var output = new MemoryStream();
        using var reader = new ITextPdfReader(input);
        using var writer = new ITextPdfWriter(output);
        using var pdf = new ITextPdfDocument(reader, writer);

        pdf.AddNewPage(ITextPageSize.A4);
    }

    [Benchmark(Description = "ZingPDF: Append a page to a mixed-workload PDF and save")]
    public async Task ZingPdf_AppendPage_AndSave_MixedWorkloadPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        using var output = new MemoryStream();

        _ = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(595, 842));
        await pdf.SaveAsync(output);
    }

    [Benchmark(Description = "ZingPDF: Append a page to a mixed-workload PDF, rewrite, and save")]
    public async Task ZingPdf_AppendPage_RewriteAndSave_MixedWorkloadPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        using var output = new MemoryStream();

        _ = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(595, 842));
        await pdf.RemoveHistoryAsync();
        await pdf.SaveAsync(output);
    }

    [Benchmark(Description = "PDFsharp: Append a page to a mixed-workload PDF and save")]
    public void PdfSharp_AppendPage_AndSave_MixedWorkloadPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Modify);
        using var output = new MemoryStream();

        pdf.AddPage(new PdfPage());
        pdf.Save(output, false);
    }

    [Benchmark(Description = "iText: Append a page to a mixed-workload PDF and save")]
    public void IText_AppendPage_AndSave_MixedWorkloadPdf()
    {
        using var input = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var output = new MemoryStream();
        using var reader = new ITextPdfReader(input);
        using var writer = new ITextPdfWriter(output);
        using var pdf = new ITextPdfDocument(reader, writer);

        pdf.AddNewPage(ITextPageSize.A4);
    }

    [Benchmark(Description = "ZingPDF: Append 10 pages to a mixed-workload PDF and save")]
    public async Task ZingPdf_Append10Pages_AndSave_MixedWorkloadPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        using var output = new MemoryStream();

        for (var i = 0; i < 10; i++)
        {
            _ = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(595, 842));
        }

        await pdf.SaveAsync(output);
    }

    [Benchmark(Description = "ZingPDF: Append 10 pages to a mixed-workload PDF, rewrite, and save")]
    public async Task ZingPdf_Append10Pages_RewriteAndSave_MixedWorkloadPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        using var output = new MemoryStream();

        for (var i = 0; i < 10; i++)
        {
            _ = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(595, 842));
        }

        await pdf.RemoveHistoryAsync();
        await pdf.SaveAsync(output);
    }

    [Benchmark(Description = "PDFsharp: Append 10 pages to a mixed-workload PDF and save")]
    public void PdfSharp_Append10Pages_AndSave_MixedWorkloadPdf()
    {
        using var stream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Modify);
        using var output = new MemoryStream();

        for (var i = 0; i < 10; i++)
        {
            pdf.AddPage(new PdfPage());
        }

        pdf.Save(output, false);
    }

    [Benchmark(Description = "iText: Append 10 pages to a mixed-workload PDF and save")]
    public void IText_Append10Pages_AndSave_MixedWorkloadPdf()
    {
        using var input = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var output = new MemoryStream();
        using var reader = new ITextPdfReader(input);
        using var writer = new ITextPdfWriter(output);
        using var pdf = new ITextPdfDocument(reader, writer);

        for (var i = 0; i < 10; i++)
        {
            pdf.AddNewPage(ITextPageSize.A4);
        }
    }

    [Benchmark(Description = "ZingPDF: Merge a minimal PDF with a second minimal PDF and save")]
    public async Task ZingPdf_Merge_MinimalPlusMinimal2_AndSave()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        using var output = new MemoryStream();
        using var appendStream = TestFiles.OpenStream(TestFiles.Minimal2);

        await pdf.AppendPdfAsync(appendStream);
        await pdf.SaveAsync(output);
    }

    [Benchmark(Description = "PDFsharp: Merge a minimal PDF with a second minimal PDF and save")]
    public void PdfSharp_Merge_MinimalPlusMinimal2_AndSave()
    {
        using var destinationStream = TestFiles.OpenStream(TestFiles.Minimal);
        using var sourceStream = TestFiles.OpenStream(TestFiles.Minimal2);
        using var pdf = PdfReader.Open(destinationStream, PdfDocumentOpenMode.Modify);
        using var source = PdfReader.Open(sourceStream, PdfDocumentOpenMode.Import);
        using var output = new MemoryStream();

        foreach (var page in source.Pages)
        {
            pdf.AddPage(page);
        }

        pdf.Save(output, false);
    }

    [Benchmark(Description = "iText: Merge a minimal PDF with a second minimal PDF and save")]
    public void IText_Merge_MinimalPlusMinimal2_AndSave()
    {
        using var destinationStream = TestFiles.OpenStream(TestFiles.Minimal);
        using var sourceStream = TestFiles.OpenStream(TestFiles.Minimal2);
        using var output = new MemoryStream();
        using var destinationReader = new ITextPdfReader(destinationStream);
        using var writer = new ITextPdfWriter(output);
        using var destination = new ITextPdfDocument(destinationReader, writer);
        using var sourceReader = new ITextPdfReader(sourceStream);
        using var source = new ITextPdfDocument(sourceReader);

        var merger = new ITextPdfMerger(destination);
        merger.Merge(source, 1, source.GetNumberOfPages());
    }

    [Benchmark(Description = "ZingPDF: Merge a text-heavy PDF into a mixed-workload PDF and save")]
    public async Task ZingPdf_AppendPdf_AndSave_MixedPlusTextHeavy()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        using var output = new MemoryStream();
        using var appendStream = TestFiles.OpenStream(TestFiles.TextHeavy);

        await pdf.AppendPdfAsync(appendStream);
        await pdf.SaveAsync(output);
    }

    [Benchmark(Description = "ZingPDF: Merge a text-heavy PDF into a mixed-workload PDF, rewrite, and save")]
    public async Task ZingPdf_AppendPdf_RewriteAndSave_MixedPlusTextHeavy()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        using var output = new MemoryStream();
        using var appendStream = TestFiles.OpenStream(TestFiles.TextHeavy);

        await pdf.AppendPdfAsync(appendStream);
        await pdf.RemoveHistoryAsync();
        await pdf.SaveAsync(output);
    }

    [Benchmark(Description = "PDFsharp: Merge a text-heavy PDF into a mixed-workload PDF and save")]
    public void PdfSharp_AppendPdf_AndSave_MixedPlusTextHeavy()
    {
        using var destinationStream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var sourceStream = TestFiles.OpenStream(TestFiles.TextHeavy);
        using var pdf = PdfReader.Open(destinationStream, PdfDocumentOpenMode.Modify);
        using var source = PdfReader.Open(sourceStream, PdfDocumentOpenMode.Import);
        using var output = new MemoryStream();

        foreach (var page in source.Pages)
        {
            pdf.AddPage(page);
        }

        pdf.Save(output, false);
    }

    [Benchmark(Description = "iText: Merge a text-heavy PDF into a mixed-workload PDF and save")]
    public void IText_AppendPdf_AndSave_MixedPlusTextHeavy()
    {
        using var destinationStream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var sourceStream = TestFiles.OpenStream(TestFiles.TextHeavy);
        using var output = new MemoryStream();
        using var destinationReader = new ITextPdfReader(destinationStream);
        using var writer = new ITextPdfWriter(output);
        using var destination = new ITextPdfDocument(destinationReader, writer);
        using var sourceReader = new ITextPdfReader(sourceStream);
        using var source = new ITextPdfDocument(sourceReader);

        var merger = new ITextPdfMerger(destination);
        merger.Merge(source, 1, source.GetNumberOfPages());
    }

    [Benchmark(Description = "ZingPDF: Export selected pages from a mixed-workload PDF and save")]
    public async Task ZingPdf_ExportPages_AndSave_MixedWorkloadPdf()
    {
        using var pdf = ZingPDF.Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        using var output = new MemoryStream();
        using var exported = await pdf.ExportPagesAsync([1, _mixedWorkloadMiddlePageNumber, _mixedWorkloadLastPageNumber]);

        await exported.SaveAsync(output);
    }

    [Benchmark(Description = "PDFsharp: Export selected pages from a mixed-workload PDF and save")]
    public void PdfSharp_ExportPages_AndSave_MixedWorkloadPdf()
    {
        using var sourceStream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var source = PdfReader.Open(sourceStream, PdfDocumentOpenMode.Import);
        using var output = new MemoryStream();
        using var destination = new PdfSharp.Pdf.PdfDocument();

        destination.AddPage(source.Pages[0]);
        destination.AddPage(source.Pages[_mixedWorkloadMiddlePageIndex]);
        destination.AddPage(source.Pages[_mixedWorkloadLastPageIndex]);
        destination.Save(output, false);
    }

    [Benchmark(Description = "iText: Export selected pages from a mixed-workload PDF and save")]
    public void IText_ExportPages_AndSave_MixedWorkloadPdf()
    {
        using var sourceStream = TestFiles.OpenStream(TestFiles.MixedWorkload);
        using var output = new MemoryStream();
        using var sourceReader = new ITextPdfReader(sourceStream);
        using var source = new ITextPdfDocument(sourceReader);
        using var writer = new ITextPdfWriter(output);
        using var destination = new ITextPdfDocument(writer);

        source.CopyPagesTo([1, _mixedWorkloadMiddlePageNumber, _mixedWorkloadLastPageNumber], destination);
    }

}
