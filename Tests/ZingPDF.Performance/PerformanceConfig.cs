using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace ZingPDF.Performance;

internal static class PerformanceConfig
{
    public static IConfig Create()
    {
        var artifactsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "artifacts", "performance", "benchmarkdotnet"));

        return ManualConfig
            .CreateMinimumViable()
            .AddJob(
                Job.ShortRun
                    .WithRuntime(CoreRuntime.Core80)
                    .WithId("Perf"))
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddExporter(MarkdownExporter.GitHub)
            .AddExporter(CsvExporter.Default)
            .AddExporter(JsonExporter.BriefCompressed)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest))
            .WithArtifactsPath(artifactsPath);
    }
}
