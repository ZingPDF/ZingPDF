using ZingPDF.Diagnostics;

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
            "realworld-count" => RunRealWorldCountAsync,
            _ => null!,
        };

        if (runner is null)
        {
            output.WriteLine($"Unknown trace scenario '{scenario}'.");
            output.WriteLine("Available scenarios: minimal-count, minimal-root, minimal-catalog, realworld-count");
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
}
