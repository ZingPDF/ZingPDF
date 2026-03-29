Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$docfxConfigPath = Join-Path $scriptRoot "api-src\docfx.json"
$outputPath = Join-Path $scriptRoot "api"
$cssOverridePath = Join-Path $scriptRoot "api-src\styles\docfx-overrides.css"

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
}
finally {
    Pop-Location
}

Write-Host "Generated DocFX API reference: $outputPath"
