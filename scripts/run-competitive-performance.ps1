param(
    [string]$Project = "Tests/ZingPDF.Performance/ZingPDF.Performance.csproj",
    [string]$OutputRoot = "artifacts/performance-competitive"
)

$ErrorActionPreference = "Stop"

$workspaceRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $workspaceRoot $Project
$outputPath = Join-Path $workspaceRoot $OutputRoot
$benchmarkArtifacts = Join-Path $outputPath "benchmarkdotnet"
$summaryJson = Join-Path $outputPath "latest-summary.json"
$summaryMarkdown = Join-Path $outputPath "latest-summary.md"
$competitiveMarkdown = Join-Path $outputPath "competitive-summary.md"

New-Item -ItemType Directory -Force -Path $benchmarkArtifacts | Out-Null

dotnet run --project $projectPath -c Release -- --filter "*CompetitiveBenchmarks*" --artifacts $benchmarkArtifacts

& (Join-Path $PSScriptRoot "write-performance-summary.ps1") `
    -BenchmarkArtifacts $benchmarkArtifacts `
    -OutputJson $summaryJson `
    -OutputMarkdown $summaryMarkdown

& (Join-Path $PSScriptRoot "write-competitive-summary.ps1") `
    -SummaryJson $summaryJson `
    -OutputMarkdown $competitiveMarkdown
