using ZingPDF.Diagnostics;
using ZingPDF.Elements.Drawing.Text.Extraction;

namespace ZingPDF.Performance;

internal static class TraceScenarios
{
    public static async Task<int> RunAsync(string scenario, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        Func<Task> runner = scenario.ToLowerInvariant() switch
        {
            "minimal-count" => RunMinimalCountAsync,
            "minimal-root" => RunMinimalRootAsync,
            "minimal-catalog" => RunMinimalCatalogAsync,
            "mixed-first-page" => RunMixedFirstPageAsync,
            "realworld-count" => RunRealWorldCountAsync,
            "textheavy-first-page-plain" => RunTextHeavyFirstPagePlainAsync,
            "textheavy-full-plain" => RunTextHeavyFullPlainAsync,
            _ => null!,
        };

        if (runner is null)
        {
            output.WriteLine($"Unknown trace scenario '{scenario}'.");
            output.WriteLine("Available scenarios: minimal-count, minimal-root, minimal-catalog, mixed-first-page, realworld-count, textheavy-first-page-plain, textheavy-full-plain");
            return 1;
        }

        // Warm the path first so the trace focuses on steady-state work rather than one-time startup.
        PerformanceTrace.SetEnabled(false);
        await runner();

        PerformanceTrace.Reset();
        PerformanceTrace.SetEnabled(true);

        try
        {
            await runner();
            output.WriteLine($"Trace scenario: {scenario}");
            PerformanceTrace.WriteSummary(output, maxEntries: 25);
            return 0;
        }
        finally
        {
            PerformanceTrace.SetEnabled(false);
        }
    }

    private static async Task RunMinimalCountAsync()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        _ = await pdf.GetPageCountAsync();
    }

    private static async Task RunMinimalRootAsync()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        _ = await pdf.Objects.PageTree.GetRootPageTreeNodeAsync();
    }

    private static async Task RunMinimalCatalogAsync()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.Minimal));
        _ = await pdf.Objects.GetDocumentCatalogAsync();
    }

    private static async Task RunRealWorldCountAsync()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.ImageHeavy));
        _ = await pdf.GetPageCountAsync();
    }

    private static async Task RunMixedFirstPageAsync()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.MixedWorkload));
        _ = await pdf.GetPageAsync(1);
    }

    private static async Task RunTextHeavyFirstPagePlainAsync()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.TextHeavy));
        _ = await pdf.ExtractTextAsync(1, new TextExtractionOptions
        {
            OutputKind = TextExtractionOutputKind.PlainText
        });
    }

    private static async Task RunTextHeavyFullPlainAsync()
    {
        using var pdf = Pdf.Load(TestFiles.OpenStream(TestFiles.TextHeavy));
        _ = await pdf.ExtractTextAsync(new TextExtractionOptions
        {
            OutputKind = TextExtractionOutputKind.PlainText
        });
    }
}
