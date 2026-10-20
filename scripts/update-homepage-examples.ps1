param(
    [string]$Python = "python",
    [string]$PdfToPpm = "pdftoppm"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$websiteRoot = Join-Path $repoRoot "website"
$assetsRoot = Join-Path $websiteRoot "assets/examples"
$runRoot = Join-Path $repoRoot (".tools/homepage-examples/" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null

Copy-Item -LiteralPath (Join-Path $repoRoot "examples/CreateProjectStatusReport/Program.cs") `
    -Destination (Join-Path $assetsRoot "project-report/Program.cs")

$projects = @(
    @{ Folder = "first-pdf"; Project = "FirstPdf.csproj" },
    @{ Folder = "project-report"; Project = "ProjectReport.csproj" }
)
foreach ($project in $projects) {
    $sourceRoot = Join-Path $assetsRoot $project.Folder
    $archive = Join-Path $assetsRoot ($project.Folder + ".zip")
    $sources = @("Program.cs", "README.md", $project.Project) | ForEach-Object { Join-Path $sourceRoot $_ }
    Compress-Archive -LiteralPath $sources -DestinationPath $archive -Force
    $workRoot = Join-Path $runRoot $project.Folder
    Expand-Archive -LiteralPath $archive -DestinationPath $workRoot
    Push-Location $workRoot
    try {
        dotnet run --project $project.Project --configuration Release
        if ($LASTEXITCODE -ne 0) { throw "Example failed: $($project.Folder)" }
    }
    finally { Pop-Location }
}

$firstOutput = Join-Path $runRoot "first-pdf/output"
foreach ($file in @("original.pdf", "edited.pdf", "extracted-text.txt")) {
    Copy-Item -LiteralPath (Join-Path $firstOutput $file) -Destination (Join-Path $assetsRoot "first-pdf/$file")
}
$reportPdf = Join-Path $websiteRoot "assets/showcase/project-status-report.pdf"
Copy-Item -LiteralPath (Join-Path $runRoot "project-report/output/project-status-report.pdf") -Destination $reportPdf

& $Python (Join-Path $PSScriptRoot "verify-homepage-examples.py") $websiteRoot
if ($LASTEXITCODE -ne 0) { throw "Independent PDF verification failed." }

foreach ($name in @("original", "edited")) {
    & $PdfToPpm -f 1 -singlefile -scale-to 480 -png (Join-Path $assetsRoot "first-pdf/$name.pdf") (Join-Path $assetsRoot "first-pdf/$name")
    if ($LASTEXITCODE -ne 0) { throw "Preview generation failed: $name" }
}
& $PdfToPpm -f 1 -singlefile -scale-to 1000 -png $reportPdf (Join-Path $websiteRoot "assets/showcase/project-status-report")
if ($LASTEXITCODE -ne 0) { throw "Report preview generation failed." }
& $PdfToPpm -f 2 -singlefile -scale-to 800 -png $reportPdf (Join-Path $runRoot "report-page-2")
if ($LASTEXITCODE -ne 0) { throw "Report second-page preview generation failed." }
& $PdfToPpm -f 2 -singlefile -scale-to 480 -png (Join-Path $assetsRoot "first-pdf/edited.pdf") (Join-Path $runRoot "edited-page-2")
if ($LASTEXITCODE -ne 0) { throw "Edited second-page preview generation failed." }
Write-Host "Examples built from downloadable archives. Inspect published previews and both second pages in $runRoot."
