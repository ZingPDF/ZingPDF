Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$docfxConfigPath = Join-Path $scriptRoot "api-src\docfx.json"
$outputPath = Join-Path $scriptRoot "api"
$cssOverridePath = Join-Path $scriptRoot "api-src\styles\docfx-overrides.css"
$apiEntryPointRelativePath = "api/ZingPDF.Pdf.html"
$apiRootIndexPath = Join-Path $outputPath "index.html"

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FromPath,

        [Parameter(Mandatory = $true)]
        [string]$ToPath
    )

    $fromUri = [System.Uri]((Resolve-Path $FromPath).Path.TrimEnd('\') + '\')
    $toUri = [System.Uri](Resolve-Path $ToPath).Path

    return [System.Uri]::UnescapeDataString($fromUri.MakeRelativeUri($toUri).ToString()).Replace('/', '/')
}

if (-not (Test-Path $docfxConfigPath)) {
    throw "DocFX config not found: $docfxConfigPath"
}

Push-Location $repoRoot
try {
    dotnet tool restore | Out-Host

    if (Test-Path $outputPath) {
        Remove-Item -Recurse -Force $outputPath
    }

    dotnet docfx $docfxConfigPath | Out-Host

    $generatedCssPath = Join-Path $outputPath "public\main.css"
    if ((Test-Path $generatedCssPath) -and (Test-Path $cssOverridePath)) {
        Add-Content -Path $generatedCssPath -Value "`r`n/* ZingPDF DocFX overrides */`r`n$(Get-Content $cssOverridePath -Raw)"
    }

    $apiEntryPointPath = Join-Path $outputPath $apiEntryPointRelativePath
    if (-not (Test-Path $apiEntryPointPath)) {
        throw "Expected API entry point page was not created: $apiEntryPointPath"
    }

    $marketingSitePath = Join-Path $scriptRoot "index.html"
    if (-not (Test-Path $marketingSitePath)) {
        throw "Expected marketing site entry point was not found: $marketingSitePath"
    }

    $rootRedirectHtml = @"
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta http-equiv="refresh" content="0; url=./$apiEntryPointRelativePath">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>ZingPDF API Reference</title>
  <link rel="canonical" href="./$apiEntryPointRelativePath">
  <script>window.location.replace('./$apiEntryPointRelativePath');</script>
</head>
<body>
  <p>Redirecting to the ZingPDF API reference entry point: <a href="./$apiEntryPointRelativePath">ZingPDF.Pdf</a>.</p>
</body>
</html>
"@

    Set-Content -Path $apiRootIndexPath -Value $rootRedirectHtml -NoNewline

    Get-ChildItem -Path $outputPath -Recurse -Filter '*.html' | ForEach-Object {
        $htmlPath = $_.FullName
        $htmlDirectory = Split-Path -Parent $htmlPath
        $marketingHref = Get-RelativePath -FromPath $htmlDirectory -ToPath $marketingSitePath
        $content = Get-Content -Path $htmlPath -Raw
        $updatedContent = [System.Text.RegularExpressions.Regex]::Replace(
            $content,
            '<a class="navbar-brand" href="[^"]+">',
            "<a class=""navbar-brand"" href=""$marketingHref"">",
            [System.Text.RegularExpressions.RegexOptions]::None)

        if ($updatedContent -ne $content) {
            Set-Content -Path $htmlPath -Value $updatedContent -NoNewline
        }
    }
}
finally {
    Pop-Location
}

Write-Host "Generated DocFX API reference: $outputPath"
