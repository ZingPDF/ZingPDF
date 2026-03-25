param(
    [Parameter(Mandatory = $true)]
    [string]$CurrentSummary,
    [Parameter(Mandatory = $true)]
    [string]$BaselineSummary,
    [Parameter(Mandatory = $true)]
    [string]$OutputMarkdown,
    [double]$ThresholdPercent = 10
)

$ErrorActionPreference = "Stop"

$current = Get-Content $CurrentSummary | ConvertFrom-Json
$baseline = Get-Content $BaselineSummary | ConvertFrom-Json

$baselineLookup = @{}
foreach ($benchmark in $baseline.benchmarks) {
    $baselineLookup[$benchmark.name] = $benchmark
}

$regressions = @()
$rows = @()

foreach ($benchmark in $current.benchmarks) {
    if (-not $baselineLookup.ContainsKey($benchmark.name)) {
        $rows += [ordered]@{
            Scenario = $benchmark.title
            Baseline = "-"
            Current = $benchmark.mean
            Delta = "new"
            Status = "no baseline"
        }
        continue
    }

    $baselineBenchmark = $baselineLookup[$benchmark.name]
    $baselineMean = [double]$baselineBenchmark.meanNanoseconds
    $currentMean = [double]$benchmark.meanNanoseconds

    if ($baselineMean -le 0) {
        continue
    }

    $deltaPercent = (($currentMean - $baselineMean) / $baselineMean) * 100
    $status = if ($deltaPercent -gt $ThresholdPercent) { "regression" } elseif ($deltaPercent -lt 0) { "improved" } else { "within threshold" }

    if ($status -eq "regression") {
        $regressions += $benchmark
    }

    $rows += [ordered]@{
        Scenario = $benchmark.title
        Baseline = $baselineBenchmark.mean
        Current = $benchmark.mean
        Delta = ("{0:N1}%" -f $deltaPercent)
        Status = $status
    }
}

$lines = @(
    "# Performance Comparison"
    ""
    "Threshold: $ThresholdPercent%"
    ""
    "| Scenario | Baseline | Current | Delta | Status |"
    "| --- | ---: | ---: | ---: | --- |"
)

foreach ($row in $rows) {
    $lines += "| $($row.Scenario) | $($row.Baseline) | $($row.Current) | $($row.Delta) | $($row.Status) |"
}

$outputDirectory = Split-Path -Parent $OutputMarkdown
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$lines | Set-Content -Path $OutputMarkdown

if ($regressions.Count -gt 0) {
    throw "Performance regressions exceeded the $ThresholdPercent% threshold. See $OutputMarkdown for details."
}
