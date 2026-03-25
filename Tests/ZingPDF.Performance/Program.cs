using BenchmarkDotNet.Running;
using ZingPDF.Performance;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, PerformanceConfig.Create());
