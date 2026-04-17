param(
    [switch]$RunBenchmarks,
    [string]$Project = "tests/ZingPDF.Performance/ZingPDF.Performance.csproj",
    [string]$OutputRoot = "artifacts/performance-site",
    [string]$PerformancePage = "website/performance.html",
    [string]$Homepage = "website/index.html",
    [string]$AccessSource = "",
    [string]$TextHeavyOpenedSource = "",
    [string]$SmallCompositeSource = "",
    [string]$WritesSource = ""
)

$ErrorActionPreference = "Stop"
$workspaceRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$BrokenMicro = [string]([char]206) + [char]188
$MicroSign = [string][char]181

function Resolve-WorkspacePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path $workspaceRoot $Path)
}

function Test-BenchmarkReportExists {
    param(
        [AllowNull()][string]$Path,
        [string]$ReportFilter = "*CompetitiveBenchmarks-report.csv"
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $resolved = Resolve-WorkspacePath $Path
    if (-not (Test-Path -LiteralPath $resolved)) {
        return $false
    }

    return $null -ne (Get-ChildItem -Path $resolved -Recurse -Filter $ReportFilter -File | Select-Object -First 1)
}

function Resolve-BenchmarkSourcePath {
    param(
        [AllowNull()][string]$ConfiguredPath,
        [Parameter(Mandatory = $true)][string]$GeneratedPath,
        [Parameter(Mandatory = $true)][string]$FallbackPath,
        [string]$ReportFilter = "*CompetitiveBenchmarks-report.csv"
    )

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPath)) {
        return $ConfiguredPath
    }

    if (Test-BenchmarkReportExists -Path $GeneratedPath -ReportFilter $ReportFilter) {
        return $GeneratedPath
    }

    return $FallbackPath
}

function Normalize-CellValue {
    param([AllowNull()][string]$Value)

    if ($null -eq $Value) {
        return $null
    }

    return $Value.
        Replace($script:BrokenMicro, "μ").
        Replace($script:MicroSign, "μ").
        Replace("&micro;", "μ").
        Trim()
}

function Convert-TimeToNanoseconds {
    param([AllowNull()][string]$Value)

    $Value = Normalize-CellValue $Value
    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -eq "-" -or $Value -eq "NA") {
        return $null
    }

    $magnitudeText = [regex]::Match($Value, "[0-9,]*\.?[0-9]+").Value
    if (-not $magnitudeText) {
        throw "Unable to parse time value '$Value'."
    }

    $magnitude = [double]($magnitudeText -replace ",", "")
    $unitText = ($Value -replace "[0-9,\.\s]", "").ToLowerInvariant()
    $unitText = $unitText.Replace($script:BrokenMicro.ToLowerInvariant(), "u")
    $unitText = $unitText.Replace("μ", "u")
    $unitText = $unitText.Replace("µ", "u")

    if ($unitText.Contains("ns")) { return $magnitude }
    if ($unitText.Contains("ms")) { return $magnitude * 1000000 }
    if ($unitText.Contains("us")) { return $magnitude * 1000 }
    if ($unitText -eq "s") { return $magnitude * 1000000000 }

    throw "Unable to parse time value '$Value'."
}

function Html-Encode {
    param([AllowNull()][string]$Value)

    if ($null -eq $Value) {
        return ""
    }

    return [System.Net.WebUtility]::HtmlEncode($Value)
}

function Format-DisplayValue {
    param([AllowNull()][string]$Value)

    $normalized = Normalize-CellValue $Value
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return ""
    }

    $encoded = Html-Encode $normalized
    $encoded = $encoded.Replace("Î¼", "&micro;")
    $encoded = $encoded.Replace("μ", "&micro;")
    $encoded = $encoded.Replace("µ", "&micro;")
    return $encoded
}

function Get-LatestCsvMetadata {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$ReportFilter = "*CompetitiveBenchmarks-report.csv"
    )

    $resolved = Resolve-WorkspacePath $Path
    $csv = Get-ChildItem -Path $resolved -Recurse -Filter $ReportFilter -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if (-not $csv) {
        throw "No BenchmarkDotNet CSV report matching '$ReportFilter' was found under $resolved."
    }

    return [pscustomobject]@{
        SourcePath = $resolved
        CsvPath = $csv.FullName
        LastWriteTimeUtc = $csv.LastWriteTimeUtc
    }
}

function Import-BenchmarkRows {
    param(
        [Parameter(Mandatory = $true)][string]$Family,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$ReportFilter = "*CompetitiveBenchmarks-report.csv"
    )

    $metadata = Get-LatestCsvMetadata -Path $Path -ReportFilter $ReportFilter
    $rawCsv = Get-Content -Path $metadata.CsvPath -Raw
    $rawCsv = $rawCsv.Replace($script:BrokenMicro, "μ").Replace($script:MicroSign, "μ")
    $rows = $rawCsv | ConvertFrom-Csv

    foreach ($row in $rows) {
        $description = ""
        if ($null -ne $row.Method) {
            $description = $row.Method.Trim("'")
        }

        if ($description -notmatch "^(ZingPDF|PDFsharp|PdfPig|iText): (?<Scenario>.+)$") {
            continue
        }

        $library = $Matches[1]
        $meanRaw = Normalize-CellValue $row.Mean
        $meanNs = Convert-TimeToNanoseconds $meanRaw

        [pscustomobject]@{
            Family = $Family
            SourceLabel = $Label
            SourceDateUtc = $metadata.LastWriteTimeUtc
            SourceCsv = $metadata.CsvPath
            Description = $description
            Library = $library
            Scenario = $Matches["Scenario"]
            MeanRaw = $meanRaw
            MeanNanoseconds = $meanNs
            Failed = [string]::Equals($meanRaw, "NA", [System.StringComparison]::OrdinalIgnoreCase)
        }
    }
}

function Add-ResultRows {
    param(
        [Parameter(Mandatory = $true)]$Rows,
        [Parameter(Mandatory = $true)][hashtable]$ResultMap
    )

    foreach ($row in $Rows) {
        $existing = $ResultMap[$row.Description]
        if ($null -eq $existing -or $row.SourceDateUtc -gt $existing.SourceDateUtc) {
            $ResultMap[$row.Description] = $row
        }
    }
}

function Invoke-CompetitiveBenchmarkFamily {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$ArtifactsPath,
        [Parameter(Mandatory = $true)][string[]]$Patterns
    )

    New-Item -ItemType Directory -Force -Path $ArtifactsPath | Out-Null

    $arguments = @(
        "run",
        "--project", $ProjectPath,
        "-c", "Release",
        "--",
        "--filter"
    )
    $arguments += $Patterns
    $arguments += @("--artifacts", $ArtifactsPath)

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Benchmark run failed for artifacts path '$ArtifactsPath'."
    }
}

function Get-ScenarioValues {
    param(
        [Parameter(Mandatory = $true)]$Scenario,
        [Parameter(Mandatory = $true)][hashtable]$ResultMap
    )

    $values = [ordered]@{}
    foreach ($library in $Scenario.Comparisons.Keys) {
        $descriptions = @($Scenario.Comparisons[$library])
        $resolved = $null
        foreach ($description in $descriptions) {
            $resolved = $ResultMap[$description]
            if ($resolved) {
                break
            }
        }
        $values[$library] = $resolved
    }

    return $values
}

function Get-FastestLibrary {
    param($ScenarioValues)

    $numeric = @($ScenarioValues.GetEnumerator() | Where-Object { $_.Value -and $_.Value.MeanNanoseconds -ne $null })
    if ($numeric.Count -eq 0) {
        return $null
    }

    return ($numeric | Sort-Object { $_.Value.MeanNanoseconds } | Select-Object -First 1).Key
}

function Get-ScenarioWinnerRow {
    param($ScenarioValues)

    $winner = Get-FastestLibrary -ScenarioValues $ScenarioValues
    if (-not $winner) {
        return $null
    }

    return $ScenarioValues[$winner]
}

function Get-BarFillClass {
    param(
        [Parameter(Mandatory = $true)][string]$Library,
        [Parameter(Mandatory = $true)][int]$Index
    )

    if ($Library -eq "ZingPDF") {
        return "zing"
    }

    if ($Index -eq 0) {
        return "alt"
    }

    return "muted"
}

function New-MetricCardHtml {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)]$ScenarioValues
    )

    $winner = Get-FastestLibrary -ScenarioValues $ScenarioValues
    if (-not $winner) {
        throw "No benchmark data is available for metric card '$Label'."
    }

    $winnerRow = $ScenarioValues[$winner]
    $message = if ($winner -eq "ZingPDF") {
        "ZingPDF fastest in the latest verified rerun."
    }
    else {
        "$winner fastest in the latest verified rerun."
    }

    return @"
          <article class="metric-card">
            <span class="metric-label">$(Html-Encode $Label)</span>
            <strong>$(Format-DisplayValue $winnerRow.MeanRaw)</strong>
            <p>$(Html-Encode $message)</p>
          </article>
"@
}

function New-BarChartHtml {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)]$ScenarioValues
    )

    $available = @($ScenarioValues.GetEnumerator() | Where-Object { $_.Value -and $_.Value.MeanNanoseconds -ne $null })
    if ($available.Count -eq 0) {
        throw "No benchmark data is available for chart '$Title'."
    }

    $maxNs = 0
    foreach ($entry in $available) {
        if ($entry.Value.MeanNanoseconds -gt $maxNs) {
            $maxNs = $entry.Value.MeanNanoseconds
        }
    }
    if ($maxNs -le 0) {
        $maxNs = 1
    }

    $rows = New-Object System.Collections.Generic.List[string]
    $index = 0
    foreach ($entry in $available | Sort-Object { $_.Value.MeanNanoseconds }) {
        $width = [math]::Round(($entry.Value.MeanNanoseconds / $maxNs) * 100, 1)
        if ($width -lt 6) {
            $width = 6
        }

        $fillClass = Get-BarFillClass -Library $entry.Key -Index $index
        $rows.Add(@"
              <div class="benchmark-bar-row">
                <span class="benchmark-bar-label">$(Html-Encode $entry.Key)</span>
                <div class="benchmark-bar-track"><span class="benchmark-bar-fill $fillClass" style="width: $width%;"></span></div>
                <span class="benchmark-bar-value">$(Format-DisplayValue $entry.Value.MeanRaw)</span>
              </div>
"@)
        $index++
    }

    return @"
          <article class="benchmark-chart-card">
            <p class="eyebrow">Snapshot</p>
            <h3>$(Html-Encode $Title)</h3>
            <div class="benchmark-bars">
$($rows -join "")
            </div>
          </article>
"@
}

function New-CompactBarsHtml {
    param(
        [Parameter(Mandatory = $true)]$ScenarioValues,
        [int]$Take = 3
    )

    $available = @($ScenarioValues.GetEnumerator() |
        Where-Object { $_.Value -and $_.Value.MeanNanoseconds -ne $null } |
        Sort-Object { $_.Value.MeanNanoseconds } |
        Select-Object -First $Take)

    if ($available.Count -eq 0) {
        throw "No compact benchmark data is available."
    }

    $maxNs = 0
    foreach ($entry in $available) {
        if ($entry.Value.MeanNanoseconds -gt $maxNs) {
            $maxNs = $entry.Value.MeanNanoseconds
        }
    }
    if ($maxNs -le 0) {
        $maxNs = 1
    }

    $rows = New-Object System.Collections.Generic.List[string]
    $index = 0
    foreach ($entry in $available) {
        $width = [math]::Round(($entry.Value.MeanNanoseconds / $maxNs) * 100, 1)
        if ($width -lt 6) {
            $width = 6
        }

        $fillClass = Get-BarFillClass -Library $entry.Key -Index $index
        $rows.Add(@"
                <div class="benchmark-bar-row compact">
                  <span class="benchmark-bar-label">$(Html-Encode $entry.Key)</span>
                  <div class="benchmark-bar-track"><span class="benchmark-bar-fill $fillClass" style="width: $width%;"></span></div>
                </div>
"@)
        $index++
    }

    return ($rows -join "")
}

function New-TableHtml {
    param(
        [Parameter(Mandatory = $true)][string]$Eyebrow,
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][string]$SupportText,
        [Parameter(Mandatory = $true)]$Scenarios,
        [Parameter(Mandatory = $true)][hashtable]$ResultMap
    )

    $libraries = New-Object System.Collections.Generic.List[string]
    foreach ($scenario in $Scenarios) {
        foreach ($library in $scenario.Comparisons.Keys) {
            if (-not $libraries.Contains($library)) {
                $libraries.Add($library)
            }
        }
    }

    $headerCells = ($libraries | ForEach-Object { "<th scope=`"col`">$(Html-Encode $_)</th>" }) -join ""
    $bodyRows = New-Object System.Collections.Generic.List[string]

    foreach ($scenario in $Scenarios) {
        $values = Get-ScenarioValues -Scenario $scenario -ResultMap $ResultMap
        $winner = Get-FastestLibrary -ScenarioValues $values

        $cells = New-Object System.Collections.Generic.List[string]
        foreach ($library in $libraries) {
            $row = $values[$library]
            if (-not $row) {
                throw "Missing benchmark row for '$library' in table '$Title'."
            }

            if ($row.Failed) {
                throw "Failed benchmark row for '$library' in table '$Title'."
            }

            $content = Format-DisplayValue $row.MeanRaw
            if ($winner -eq $library) {
                $cells.Add("<td><span class=`"benchmark-win`">$content</span></td>")
            }
            else {
                $cells.Add("<td>$content</td>")
            }
        }

        $bodyRows.Add(@"
                  <tr>
                    <td>$(Html-Encode $scenario.Label)</td>
$($cells -join "")
                  </tr>
"@)
    }

    return @"
          <section class="support-group">
            <div class="support-group-copy">
              <p class="eyebrow">$(Html-Encode $Eyebrow)</p>
              <h3>$(Html-Encode $Title)</h3>
              <p>$(Html-Encode $SupportText)</p>
            </div>
            <div class="support-table-wrap">
              <table class="support-table benchmark-table">
                <thead>
                  <tr>
                    <th scope="col">Workload</th>
$headerCells
                  </tr>
                </thead>
                <tbody>
$($bodyRows -join "")
                </tbody>
              </table>
            </div>
          </section>
"@
}

function Set-GeneratedBlock {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$Replacement
    )

    $pattern = "(?s)(<!-- AUTO-GENERATED:$([regex]::Escape($Key)):start -->)(.*?)(<!-- AUTO-GENERATED:$([regex]::Escape($Key)):end -->)"
    if ($Content -notmatch $pattern) {
        throw "Marker block '$Key' was not found."
    }

    return [regex]::Replace($Content, $pattern, "`$1`r`n$Replacement`r`n`$3", 1)
}

function Normalize-GeneratedHtml {
    param([Parameter(Mandatory = $true)][string]$Content)

    return $Content.
        Replace("&#206;&#188;", "&micro;").
        Replace("ÃŽÂ¼", "&micro;").
        Replace("Î¼", "&micro;").
        Replace("Âµ", "&micro;")
}

function Assert-ScenarioSetComplete {
    param(
        [Parameter(Mandatory = $true)][string]$GroupName,
        [Parameter(Mandatory = $true)]$Scenarios,
        [Parameter(Mandatory = $true)][hashtable]$ResultMap,
        [switch]$BenchmarksWereRun
    )

    $issues = New-Object System.Collections.Generic.List[string]

    foreach ($scenario in $Scenarios) {
        foreach ($library in $scenario.Comparisons.Keys) {
            $descriptions = @($scenario.Comparisons[$library])
            $description = $descriptions[0]
            $row = $null
            foreach ($candidate in $descriptions) {
                $row = $ResultMap[$candidate]
                if ($row) {
                    $description = $candidate
                    break
                }
            }

            if (-not $row) {
                $issues.Add("${GroupName}: missing '$description'")
                continue
            }

            if ($row.Failed -or $null -eq $row.MeanNanoseconds) {
                $issues.Add("${GroupName}: failed '$description'")
            }
        }
    }

    if ($issues.Count -gt 0) {
        $reason = if ($BenchmarksWereRun) {
            "Benchmark page generation aborted because the freshly run suite still contains failing or incomplete results."
        }
        else {
            "Benchmark page generation aborted because the cached result set is incomplete."
        }

        $guidance = if ($BenchmarksWereRun) {
            "Fix the failing benchmark scenarios below and rerun the suite before publishing performance content."
        }
        else {
            "Run the script with -RunBenchmarks to refresh the benchmark cache, or point the source parameters at a complete result set."
        }

        throw "$reason`n$guidance`n - $($issues -join "`n - ")"
    }
}

function Assert-SingleBenchmarkDate {
    param(
        [Parameter(Mandatory = $true)]$Metadatas
    )

    $dates = @($Metadatas | ForEach-Object { $_.LastWriteTimeUtc.ToLocalTime().ToString('yyyy-MM-dd') } | Select-Object -Unique)
    if ($dates.Count -ne 1) {
        throw "Benchmark page generation aborted because benchmark artifacts come from multiple run dates: $($dates -join ', '). Rerun the full suite so the site can report one latest verified date."
    }

    return $dates[0]
}

$projectPath = Resolve-WorkspacePath $Project
$generatedRoot = Resolve-WorkspacePath $OutputRoot
$defaultAccessGenerated = Join-Path $generatedRoot "access\benchmarkdotnet"
$defaultExtractionGenerated = Join-Path $generatedRoot "extraction\benchmarkdotnet"
$defaultWritesGenerated = Join-Path $generatedRoot "writes\benchmarkdotnet"

$AccessSource = Resolve-BenchmarkSourcePath `
    -ConfiguredPath $AccessSource `
    -GeneratedPath $defaultAccessGenerated `
    -FallbackPath "artifacts/performance-competitive-access"

$TextHeavyOpenedSource = Resolve-BenchmarkSourcePath `
    -ConfiguredPath $TextHeavyOpenedSource `
    -GeneratedPath $defaultExtractionGenerated `
    -FallbackPath "artifacts/performance-competitive-textheavy-opened-smoke-after"

$SmallCompositeSource = Resolve-BenchmarkSourcePath `
    -ConfiguredPath $SmallCompositeSource `
    -GeneratedPath $defaultExtractionGenerated `
    -FallbackPath "artifacts/performance-competitive-text-smoke-after"

$WritesSource = Resolve-BenchmarkSourcePath `
    -ConfiguredPath $WritesSource `
    -GeneratedPath $defaultWritesGenerated `
    -FallbackPath "tests/artifacts/performance/benchmarkdotnet"

if ($RunBenchmarks) {
    $accessArtifacts = Join-Path $generatedRoot "access\benchmarkdotnet"
    $extractionArtifacts = Join-Path $generatedRoot "extraction\benchmarkdotnet"
    $writesArtifacts = Join-Path $generatedRoot "writes\benchmarkdotnet"

    Invoke-CompetitiveBenchmarkFamily -ProjectPath $projectPath -ArtifactsPath $accessArtifacts -Patterns @(
        "*Open_MinimalPdf*",
        "*Open_RealWorldPdf*",
        "*CountPages_RealWorldPdf*",
        "*GetFirstPage_MixedWorkloadPdf*",
        "*GetMiddlePage_MixedWorkloadPdf*",
        "*GetLastPage_MixedWorkloadPdf*"
    )

    Invoke-CompetitiveBenchmarkFamily -ProjectPath $projectPath -ArtifactsPath $extractionArtifacts -Patterns @(
        "*ExtractText_TextHeavyPdf*",
        "*ExtractText_FirstPage_TextHeavyPdf*",
        "*ExtractText_FirstPage_TextHeavyPdf_Opened*",
        "*ExtractText_TestPdf*",
        "*ExtractText_FirstPage_TestPdf*",
        "*ExtractText_FirstPage_TestPdf_Opened*"
    )

    Invoke-CompetitiveBenchmarkFamily -ProjectPath $projectPath -ArtifactsPath $writesArtifacts -Patterns @(
        "*AppendPage_RewriteAndSave_MixedWorkloadPdf*",
        "*Append10Pages_RewriteAndSave_MixedWorkloadPdf*",
        "*AppendPdf_RewriteAndSave_MixedPlusTextHeavy*",
        "*PdfSharp_AppendPage_AndSave_MixedWorkloadPdf*",
        "*PdfSharp_Append10Pages_AndSave_MixedWorkloadPdf*",
        "*PdfSharp_AppendPdf_AndSave_MixedPlusTextHeavy*",
        "*IText_AppendPage_AndSave_MixedWorkloadPdf*",
        "*IText_Append10Pages_AndSave_MixedWorkloadPdf*",
        "*IText_AppendPdf_AndSave_MixedPlusTextHeavy*"
    )

    $AccessSource = $accessArtifacts
    $TextHeavyOpenedSource = $extractionArtifacts
    $SmallCompositeSource = $extractionArtifacts
    $WritesSource = $writesArtifacts
}

$allResults = @{}
Add-ResultRows -ResultMap $allResults -Rows (Import-BenchmarkRows -Family "access" -Label "Access" -Path $AccessSource)
Add-ResultRows -ResultMap $allResults -Rows (Import-BenchmarkRows -Family "extraction" -Label "Text-heavy and access" -Path $AccessSource)
Add-ResultRows -ResultMap $allResults -Rows (Import-BenchmarkRows -Family "extraction" -Label "Text-heavy already-open" -Path $TextHeavyOpenedSource)
Add-ResultRows -ResultMap $allResults -Rows (Import-BenchmarkRows -Family "extraction" -Label "Small composite-font" -Path $SmallCompositeSource)
Add-ResultRows -ResultMap $allResults -Rows (Import-BenchmarkRows -Family "writes" -Label "Writes" -Path $WritesSource)

$accessMetadata = Get-LatestCsvMetadata -Path $AccessSource
$textHeavyOpenedMetadata = Get-LatestCsvMetadata -Path $TextHeavyOpenedSource
$smallCompositeMetadata = Get-LatestCsvMetadata -Path $SmallCompositeSource
$writesMetadata = Get-LatestCsvMetadata -Path $WritesSource

$accessScenarios = @(
    @{
        Label = "Open minimal PDF"
        Comparisons = [ordered]@{
            ZingPDF = "ZingPDF: Open a minimal PDF"
            PDFsharp = "PDFsharp: Open a minimal PDF"
            PdfPig = "PdfPig: Open a minimal PDF"
            iText = "iText: Open a minimal PDF"
        }
    },
    @{
        Label = "Open larger real-world PDF"
        Comparisons = [ordered]@{
            ZingPDF = "ZingPDF: Open a larger real-world PDF"
            PDFsharp = "PDFsharp: Open a larger real-world PDF"
            PdfPig = "PdfPig: Open a larger real-world PDF"
            iText = "iText: Open a larger real-world PDF"
        }
    },
    @{
        Label = "Open and count pages, larger real-world PDF"
        Comparisons = [ordered]@{
            ZingPDF = "ZingPDF: Open and count pages in a larger real-world PDF"
            PDFsharp = "PDFsharp: Open and count pages in a larger real-world PDF"
            PdfPig = "PdfPig: Open and count pages in a larger real-world PDF"
            iText = "iText: Open and count pages in a larger real-world PDF"
        }
    },
    @{
        Label = "Open and get first page, mixed workload"
        Comparisons = [ordered]@{
            ZingPDF = "ZingPDF: Open and get the first page in a mixed-workload PDF"
            PDFsharp = "PDFsharp: Open and get the first page in a mixed-workload PDF"
            PdfPig = "PdfPig: Open and get the first page in a mixed-workload PDF"
            iText = "iText: Open and get the first page in a mixed-workload PDF"
        }
    },
    @{
        Label = "Open and get middle page, mixed workload"
        Comparisons = [ordered]@{
            ZingPDF = "ZingPDF: Open and get the middle page in a mixed-workload PDF"
            PDFsharp = "PDFsharp: Open and get the middle page in a mixed-workload PDF"
            PdfPig = "PdfPig: Open and get the middle page in a mixed-workload PDF"
            iText = "iText: Open and get the middle page in a mixed-workload PDF"
        }
    },
    @{
        Label = "Open and get last page, mixed workload"
        Comparisons = [ordered]@{
            ZingPDF = "ZingPDF: Open and get the last page in a mixed-workload PDF"
            PDFsharp = "PDFsharp: Open and get the last page in a mixed-workload PDF"
            PdfPig = "PdfPig: Open and get the last page in a mixed-workload PDF"
            iText = "iText: Open and get the last page in a mixed-workload PDF"
        }
    }
)

$extractionScenarios = @(
    @{
        Label = "Open and extract first-page plain text, text-heavy PDF"
        Comparisons = [ordered]@{
            ZingPDF = @("ZingPDF: Open and extract text from the first page in a text-heavy PDF", "ZingPDF: Open and extract plain text from the first page in a text-heavy PDF")
            PdfPig = @("PdfPig: Open and extract text from the first page in a text-heavy PDF", "PdfPig: Open and extract plain text from the first page in a text-heavy PDF")
            iText = @("iText: Open and extract text from the first page in a text-heavy PDF", "iText: Open and extract plain text from the first page in a text-heavy PDF")
        }
    },
    @{
        Label = "Extract first-page plain text, already-open text-heavy PDF"
        Comparisons = [ordered]@{
            ZingPDF = @("ZingPDF: Extract text from the first page in an already-open text-heavy PDF", "ZingPDF: Extract plain text from the first page in an already-open text-heavy PDF")
            PdfPig = @("PdfPig: Extract text from the first page in an already-open text-heavy PDF", "PdfPig: Extract plain text from the first page in an already-open text-heavy PDF")
            iText = @("iText: Extract text from the first page in an already-open text-heavy PDF", "iText: Extract plain text from the first page in an already-open text-heavy PDF")
        }
    },
    @{
        Label = "Extract full-document plain text, text-heavy PDF"
        Comparisons = [ordered]@{
            ZingPDF = @("ZingPDF: Extract text from a text-heavy PDF", "ZingPDF: Extract plain text from a text-heavy PDF")
            PdfPig = @("PdfPig: Extract text from a text-heavy PDF", "PdfPig: Extract plain text from a text-heavy PDF")
            iText = @("iText: Extract text from a text-heavy PDF", "iText: Extract plain text from a text-heavy PDF")
        }
    },
    @{
        Label = "Open and extract first-page plain text, small composite-font PDF"
        Comparisons = [ordered]@{
            ZingPDF = @("ZingPDF: Open and extract text from the first page in a small composite-font PDF", "ZingPDF: Open and extract plain text from the first page in a small composite-font PDF")
            PdfPig = @("PdfPig: Open and extract text from the first page in a small composite-font PDF", "PdfPig: Open and extract plain text from the first page in a small composite-font PDF")
            iText = @("iText: Open and extract text from the first page in a small composite-font PDF", "iText: Open and extract plain text from the first page in a small composite-font PDF")
        }
    },
    @{
        Label = "Extract first-page plain text, already-open small composite-font PDF"
        Comparisons = [ordered]@{
            ZingPDF = @("ZingPDF: Extract text from the first page in an already-open small composite-font PDF", "ZingPDF: Extract plain text from the first page in an already-open small composite-font PDF")
            PdfPig = @("PdfPig: Extract text from the first page in an already-open small composite-font PDF", "PdfPig: Extract plain text from the first page in an already-open small composite-font PDF")
            iText = @("iText: Extract text from the first page in an already-open small composite-font PDF", "iText: Extract plain text from the first page in an already-open small composite-font PDF")
        }
    }
)

$writeScenarios = @(
    @{
        Label = "Append 1 page to mixed-workload PDF and save"
        Comparisons = [ordered]@{
            ZingPDF = "ZingPDF: Append a page to a mixed-workload PDF, rewrite, and save"
            PDFsharp = "PDFsharp: Append a page to a mixed-workload PDF and save"
            iText = "iText: Append a page to a mixed-workload PDF and save"
        }
    },
    @{
        Label = "Append 10 pages to mixed-workload PDF and save"
        Comparisons = [ordered]@{
            ZingPDF = "ZingPDF: Append 10 pages to a mixed-workload PDF, rewrite, and save"
            PDFsharp = "PDFsharp: Append 10 pages to a mixed-workload PDF and save"
            iText = "iText: Append 10 pages to a mixed-workload PDF and save"
        }
    },
    @{
        Label = "Merge text-heavy PDF into mixed-workload PDF and save"
        Comparisons = [ordered]@{
            ZingPDF = "ZingPDF: Merge a text-heavy PDF into a mixed-workload PDF, rewrite, and save"
            PDFsharp = "PDFsharp: Merge a text-heavy PDF into a mixed-workload PDF and save"
            iText = "iText: Merge a text-heavy PDF into a mixed-workload PDF and save"
        }
    }
)

Assert-ScenarioSetComplete -GroupName "Access" -Scenarios $accessScenarios -ResultMap $allResults -BenchmarksWereRun:$RunBenchmarks
Assert-ScenarioSetComplete -GroupName "Extraction" -Scenarios $extractionScenarios -ResultMap $allResults -BenchmarksWereRun:$RunBenchmarks
Assert-ScenarioSetComplete -GroupName "Writes" -Scenarios $writeScenarios -ResultMap $allResults -BenchmarksWereRun:$RunBenchmarks

$latestVerifiedDateKey = Assert-SingleBenchmarkDate -Metadatas @(
    $accessMetadata,
    $textHeavyOpenedMetadata,
    $smallCompositeMetadata,
    $writesMetadata
)
$latestVerifiedDate = [datetime]::ParseExact($latestVerifiedDateKey, 'yyyy-MM-dd', [System.Globalization.CultureInfo]::InvariantCulture).ToString('d MMM yyyy')

$firstPageAccessValues = Get-ScenarioValues -Scenario $accessScenarios[3] -ResultMap $allResults
$textHeavyFirstPageValues = Get-ScenarioValues -Scenario $extractionScenarios[0] -ResultMap $allResults
$appendTenValues = Get-ScenarioValues -Scenario $writeScenarios[1] -ResultMap $allResults
$mergeValues = Get-ScenarioValues -Scenario $writeScenarios[2] -ResultMap $allResults
$openMinimalValues = Get-ScenarioValues -Scenario $accessScenarios[0] -ResultMap $allResults

$firstPageAccessWinnerRow = Get-ScenarioWinnerRow -ScenarioValues $firstPageAccessValues
$appendTenWinnerRow = Get-ScenarioWinnerRow -ScenarioValues $appendTenValues

$performanceHeroCallout = @"
        <article class="doc-card benchmark-callout">
          <h3>Latest verified snapshots</h3>
          <p>Performance benchmarks last run on $latestVerifiedDate.</p>
        </article>
"@

$performanceResults = @"
      <section class="doc-section benchmark-section" id="results">
        <p class="eyebrow">Results</p>
        <h2>Latest verified benchmark snapshots by workload family</h2>

        <div class="metric-grid">
$(New-MetricCardHtml -Label "Open minimal PDF" -ScenarioValues $openMinimalValues)
$(New-MetricCardHtml -Label "Text-heavy first-page extraction" -ScenarioValues $textHeavyFirstPageValues)
$(New-MetricCardHtml -Label "Append 10 pages and save" -ScenarioValues $appendTenValues)
$(New-MetricCardHtml -Label "Large merge" -ScenarioValues $mergeValues)
        </div>

        <section class="benchmark-chart-grid" aria-label="Benchmark snapshots">
$(New-BarChartHtml -Title "First-page access, mixed workload" -ScenarioValues $firstPageAccessValues)
$(New-BarChartHtml -Title "First-page text extraction, text-heavy PDF" -ScenarioValues $textHeavyFirstPageValues)
$(New-BarChartHtml -Title "Append 10 pages and save" -ScenarioValues $appendTenValues)
$(New-BarChartHtml -Title "Large merge" -ScenarioValues $mergeValues)
        </section>

        <div class="support-groups">
$(New-TableHtml -Eyebrow "Access" -Title "Seekable stream access and page lookup" -SupportText "Latest verified benchmark run: $latestVerifiedDate." -Scenarios $accessScenarios -ResultMap $allResults)
$(New-TableHtml -Eyebrow "Extraction" -Title "Plain-text extraction across current fixtures" -SupportText "Latest verified benchmark run: $latestVerifiedDate." -Scenarios $extractionScenarios -ResultMap $allResults)
$(New-TableHtml -Eyebrow "Writes" -Title "Append-heavy edits and larger merge saves" -SupportText "Latest verified benchmark run: $latestVerifiedDate." -Scenarios $writeScenarios -ResultMap $allResults)
        </div>
      </section>
"@

$homepagePerformance = @"
      <section class="homepage-performance-band" aria-labelledby="performance-summary-title">
        <div class="homepage-performance-shell">
          <div class="section-copy">
            <p class="eyebrow">Performance</p>
            <h2 id="performance-summary-title">Ongoing transparent performance improvement</h2>
          </div>

          <div class="homepage-performance-grid">
            <article class="homepage-performance-card">
              <span class="metric-label">First-page access</span>
              <strong>$(Format-DisplayValue $firstPageAccessWinnerRow.MeanRaw)</strong>
              <div class="benchmark-bars compact">
$(New-CompactBarsHtml -ScenarioValues $firstPageAccessValues)
              </div>
            </article>

            <article class="homepage-performance-card">
              <span class="metric-label">Append 10 pages and save</span>
              <strong>$(Format-DisplayValue $appendTenWinnerRow.MeanRaw)</strong>
              <div class="benchmark-bars compact">
$(New-CompactBarsHtml -ScenarioValues $appendTenValues)
              </div>
            </article>

            <article class="homepage-performance-card homepage-performance-cta">
              <span class="metric-label">Detailed comparison</span>
              <strong>Latest verified performance tests</strong>
              <p>See the benchmark methodology and the full multi-library comparison.</p>
              <a class="button button-performance" href="./performance.html">View performance page</a>
            </article>
          </div>
        </div>
      </section>
"@

$performancePagePath = Resolve-WorkspacePath $PerformancePage
$performanceContent = Get-Content -Path $performancePagePath -Raw
$performanceContent = Set-GeneratedBlock -Content $performanceContent -Key "performance-hero-callout" -Replacement $performanceHeroCallout.TrimEnd()
$performanceContent = Set-GeneratedBlock -Content $performanceContent -Key "performance-results" -Replacement $performanceResults.TrimEnd()
$performanceContent = Normalize-GeneratedHtml -Content $performanceContent
Set-Content -Path $performancePagePath -Value $performanceContent

$homepagePath = Resolve-WorkspacePath $Homepage
$homepageContent = Get-Content -Path $homepagePath -Raw
$homepageContent = Set-GeneratedBlock -Content $homepageContent -Key "homepage-performance" -Replacement $homepagePerformance.TrimEnd()
$homepageContent = Normalize-GeneratedHtml -Content $homepageContent
Set-Content -Path $homepagePath -Value $homepageContent

Write-Host "Updated:"
Write-Host " - $performancePagePath"
Write-Host " - $homepagePath"
