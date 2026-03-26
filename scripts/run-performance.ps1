param(
    [string]$Project = "Tests/ZingPDF.Performance/ZingPDF.Performance.csproj",
    [string]$OutputRoot = "artifacts/performance",
    [string]$Filter = "*PdfBenchmarks*",
    [string]$ThresholdPercent = "10",
    [string]$BaselineSummary = "",
    [switch]$CompareToBaseline
)

$ErrorActionPreference = "Stop"

$workspaceRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $workspaceRoot $Project
$outputPath = Join-Path $workspaceRoot $OutputRoot
$benchmarkArtifacts = Join-Path $outputPath "benchmarkdotnet"
$summaryJson = Join-Path $outputPath "latest-summary.json"
$summaryMarkdown = Join-Path $outputPath "latest-summary.md"
$comparisonMarkdown = Join-Path $outputPath "comparison.md"

New-Item -ItemType Directory -Force -Path $benchmarkArtifacts | Out-Null

dotnet run --project $projectPath -c Release -- --filter $Filter --artifacts $benchmarkArtifacts

& (Join-Path $PSScriptRoot "write-performance-summary.ps1") `
    -BenchmarkArtifacts $benchmarkArtifacts `
    -OutputJson $summaryJson `
    -OutputMarkdown $summaryMarkdown

if ($CompareToBaseline) {
    if ([string]::IsNullOrWhiteSpace($BaselineSummary)) {
        throw "BaselineSummary must be provided when CompareToBaseline is set."
    }

    & (Join-Path $PSScriptRoot "compare-performance.ps1") `
        -CurrentSummary $summaryJson `
        -BaselineSummary $BaselineSummary `
        -OutputMarkdown $comparisonMarkdown `
        -ThresholdPercent $ThresholdPercent
}
