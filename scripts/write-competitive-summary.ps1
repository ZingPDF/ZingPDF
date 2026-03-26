param(
    [Parameter(Mandatory = $true)]
    [string]$SummaryJson,
    [Parameter(Mandatory = $true)]
    [string]$OutputMarkdown
)

$ErrorActionPreference = "Stop"

$summary = Get-Content $SummaryJson | ConvertFrom-Json

function Parse-BenchmarkName {
    param([string]$Name)

    $parts = $Name.Split(":", 2)
    if ($parts.Count -ne 2) {
        throw "Competitive benchmark name '$Name' must use the format 'Library: Scenario'."
    }

    return [ordered]@{
        Library = $parts[0].Trim()
        Scenario = $parts[1].Trim()
    }
}

function Get-TargetLabel {
    param(
        [double]$RatioToFastest,
        [bool]$IsWinner
    )

    if ($IsWinner) { return "Hold or improve" }
    if ($RatioToFastest -le 1.5) { return "Competitive" }
    if ($RatioToFastest -le 3.0) { return "Good next target: within 50%" }
    return "Needs major work: within 2x first"
}

$grouped = @{}
foreach ($benchmark in $summary.benchmarks) {
    if (-not $benchmark.meanNanoseconds) {
        continue
    }

    $parsed = Parse-BenchmarkName $benchmark.name
    if (-not $grouped.ContainsKey($parsed.Scenario)) {
        $grouped[$parsed.Scenario] = @()
    }

    $grouped[$parsed.Scenario] += [pscustomobject]@{
        Library = $parsed.Library
        Scenario = $parsed.Scenario
        Mean = $benchmark.mean
        MeanNanoseconds = [double]$benchmark.meanNanoseconds
        Allocated = if ($benchmark.allocated) { $benchmark.allocated } else { "-" }
    }
}

$lines = @(
    "# Competitive Performance Snapshot"
    ""
    "Generated: $($summary.generatedAtUtc)"
    ""
    "Environment:"
    "- OS: $($summary.environment.osDescription)"
    "- Architecture: $($summary.environment.osArchitecture)"
    "- Benchmark runtime: $($summary.environment.benchmarkRuntime)"
    "- Machine: $($summary.environment.machineName)"
    ""
)

foreach ($scenario in ($grouped.Keys | Sort-Object)) {
    $entries = @($grouped[$scenario] | Sort-Object MeanNanoseconds)
    $winner = $entries[0]
    $zing = $entries | Where-Object Library -eq "ZingPDF" | Select-Object -First 1

    $lines += "## $scenario"
    $lines += ""
    $lines += "| Library | Mean | Allocated | Relative to Fastest |"
    $lines += "| --- | ---: | ---: | ---: |"

    foreach ($entry in $entries) {
        $relative = $entry.MeanNanoseconds / $winner.MeanNanoseconds
        $lines += "| $($entry.Library) | $($entry.Mean) | $($entry.Allocated) | $("{0:N2}x" -f $relative) |"
    }

    if ($null -ne $zing) {
        $zingRatio = $zing.MeanNanoseconds / $winner.MeanNanoseconds
        $targetLabel = Get-TargetLabel -RatioToFastest $zingRatio -IsWinner:($zing.Library -eq $winner.Library)
        $lines += ""
        $lines += "Target for ZingPDF: $targetLabel."
    }

    $lines += ""
}

$outputDirectory = Split-Path -Parent $OutputMarkdown
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$lines | Set-Content -Path $OutputMarkdown
