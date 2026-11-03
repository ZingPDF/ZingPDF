param(
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'osx-arm64', 'osx-x64')]
    [string]$Runtime = 'win-x64'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'apps/ZingPDF.Editor/ZingPDF.Editor.csproj'
$output = Join-Path $repoRoot "artifacts/editor/$Runtime"
& dotnet publish $project --configuration Release --runtime $Runtime --self-contained true --output $output -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw "Editor publish failed with code $LASTEXITCODE." }
Copy-Item -LiteralPath (Join-Path $repoRoot 'apps/ZingPDF.Editor/README.md') -Destination (Join-Path $output 'README.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE.txt') -Destination (Join-Path $output 'ZingPDF-LICENSE.txt')
Write-Host "Published editor to $output"
if ($Runtime -ne 'win-x64') { Write-Host 'This target needs platform-specific runtime validation before distribution.' }
