param(
    [Parameter(Mandatory = $true)]
    [string]$BenchmarkArtifacts,
    [Parameter(Mandatory = $true)]
    [string]$OutputJson,
    [Parameter(Mandatory = $true)]
    [string]$OutputMarkdown
)

$ErrorActionPreference = "Stop"

function Normalize-Value {
    param([string]$Value)

    if ($null -eq $Value) {
        return $Value
    }

    return $Value.Trim()
}

function Convert-TimeToNanoseconds {
    param([string]$Value)

    $Value = Normalize-Value $Value

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -eq "-" -or $Value -eq "NA") {
        return $null
    }

    $magnitudeText = [regex]::Match($Value, "[0-9,]*\.?[0-9]+").Value
    if (-not $magnitudeText) {
        throw "Unable to parse time value '$Value'."
    }

    $magnitude = [double]($magnitudeText -replace ",", "")
    $unitText = ($Value -replace "[0-9,\.\s]", "").ToLowerInvariant()

    if ($unitText.Contains("ns")) { return $magnitude }
    if ($unitText.Contains("ms")) { return $magnitude * 1000000 }
    if ($unitText.Contains("us")) { return $magnitude * 1000 }
    if ($unitText -eq "s") { return $magnitude * 1000000000 }
    if ($unitText.EndsWith("s")) { return $magnitude * 1000 }

    throw "Unable to parse time value '$Value'."
}

function Convert-SizeToBytes {
    param([string]$Value)

    $Value = Normalize-Value $Value

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -eq "-" -or $Value -eq "NA") {
        return $null
    }

    $magnitudeText = [regex]::Match($Value, "[0-9,]*\.?[0-9]+").Value
    if (-not $magnitudeText) {
        throw "Unable to parse size value '$Value'."
    }

    $magnitude = [double]($magnitudeText -replace ",", "")

    if ($Value.Contains("GB")) { return [math]::Round($magnitude * 1GB, 2) }
    if ($Value.Contains("MB")) { return [math]::Round($magnitude * 1MB, 2) }
    if ($Value.Contains("KB")) { return [math]::Round($magnitude * 1KB, 2) }
    if ($Value.Contains("B")) { return [math]::Round($magnitude, 2) }

    throw "Unable to parse size value '$Value'."
}

$csvPath = Get-ChildItem -Path $BenchmarkArtifacts -Recurse -Filter "*-report.csv" |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $csvPath) {
    throw "No BenchmarkDotNet CSV report was found under $BenchmarkArtifacts."
}

$rows = Import-Csv -Path $csvPath
$timestamp = [DateTime]::UtcNow.ToString("o")
$benchmarkRuntime = if ($rows.Count -gt 0) { $rows[0].Runtime } else { $null }

$benchmarks = @(
    foreach ($row in $rows) {
        [ordered]@{
            name = $row.Method.Trim("'")
            title = $row.Method.Trim("'")
            mean = Normalize-Value $row.Mean
            meanNanoseconds = Convert-TimeToNanoseconds $row.Mean
            error = Normalize-Value $row.Error
            stdDev = Normalize-Value $row.StdDev
            allocated = Normalize-Value $row.Allocated
            allocatedBytes = Convert-SizeToBytes $row.Allocated
            gen0 = $row.Gen0
            gen1 = $row.Gen1
            gen2 = $row.Gen2
        }
    }
)

$summary = [ordered]@{
    generatedAtUtc = $timestamp
    sourceReport = $csvPath
    environment = [ordered]@{
        osDescription = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        osArchitecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
        processArchitecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
        benchmarkRuntime = $benchmarkRuntime
        framework = [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
        machineName = $env:COMPUTERNAME
        ci = [bool]$env:CI
        githubRunId = $env:GITHUB_RUN_ID
        githubSha = $env:GITHUB_SHA
    }
    benchmarks = $benchmarks
}

$summaryDirectory = Split-Path -Parent $OutputJson
if ($summaryDirectory) {
    New-Item -ItemType Directory -Force -Path $summaryDirectory | Out-Null
}

$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputJson

$lines = @(
    "# ZingPDF Performance Snapshot"
    ""
    "Generated: $timestamp"
    ""
    "Environment:"
    "- OS: $($summary.environment.osDescription)"
    "- Architecture: $($summary.environment.osArchitecture)"
    "- Benchmark runtime: $($summary.environment.benchmarkRuntime)"
    "- Machine: $($summary.environment.machineName)"
    ""
    "| Scenario | Mean | Allocated |"
    "| --- | ---: | ---: |"
)

foreach ($benchmark in $benchmarks) {
    $allocated = if ($benchmark.allocated) { $benchmark.allocated } else { "-" }
    $lines += "| $($benchmark.title) | $($benchmark.mean) | $allocated |"
}

$lines += ""
$lines += "Use this snapshot for release notes, performance changelogs, and marketing proof points. Include the environment details whenever you publish the numbers."

$markdownDirectory = Split-Path -Parent $OutputMarkdown
if ($markdownDirectory) {
    New-Item -ItemType Directory -Force -Path $markdownDirectory | Out-Null
}

$lines | Set-Content -Path $OutputMarkdown
