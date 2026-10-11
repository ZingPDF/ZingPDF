using System.Diagnostics;
using System.Reflection;
using FluentAssertions;
using Xunit;
using ZingPDF.Logging;

namespace ZingPDF.Tests.Unit.Logging;

public sealed class LoggerInitializationTests
{
    private const string ProbeEnvironmentVariable = "ZINGPDF_LOGGER_INITIALIZATION_PROBE";
    private const string ProbeTestName = "ZingPDF.Tests.Unit.Logging.LoggerInitializationTests.LoggerDefaultInitializationProbe";

    [Fact]
    public async Task DefaultInitialization_DoesNotCreateLogFileInCurrentDirectory()
    {
        var workingDirectory = Directory.CreateTempSubdirectory("zingpdf-logger-");
        try
        {
            var testAssembly = Assembly.GetExecutingAssembly().Location;
            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = workingDirectory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("vstest");
            startInfo.ArgumentList.Add(testAssembly);
            startInfo.ArgumentList.Add($"--TestCaseFilter:FullyQualifiedName={ProbeTestName}");
            startInfo.Environment[ProbeEnvironmentVariable] = "1";

            using var process = Process.Start(startInfo);
            process.Should().NotBeNull();
            var standardOutput = process!.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            try
            {
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (TimeoutException)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                throw new TimeoutException("The isolated logger initialization probe did not finish within 30 seconds.");
            }

            var output = await standardOutput;
            var error = await standardError;
            process.ExitCode.Should().Be(0, $"the isolated probe must pass; stdout: {output}\nstderr: {error}");
            Directory.GetFiles(workingDirectory.FullName, "debug-log-*.log").Should().BeEmpty();
        }
        finally
        {
            workingDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void LoggerDefaultInitializationProbe()
    {
        if (Environment.GetEnvironmentVariable(ProbeEnvironmentVariable) != "1")
        {
            return;
        }

        Logger.LogLevel.Should().Be(LogLevel.Error);
        Logger.Log(LogLevel.Trace, "default logging is disabled");
    }
}
