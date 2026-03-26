using BenchmarkDotNet.Running;
using ZingPDF.Performance;

if (args.Length >= 2 && args[0].Equals("--trace", StringComparison.OrdinalIgnoreCase))
{
    return await TraceScenarios.RunAsync(args[1], Console.Out);
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, PerformanceConfig.Create());
return 0;
