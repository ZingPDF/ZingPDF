param(
    [string]$PdfPath,
    [switch]$Demo
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'apps/ZingPDF.Editor/ZingPDF.Editor.csproj'
$editorArgs = @('run', '--project', $project, '--configuration', 'Release', '--')
if ($Demo) { $editorArgs += '--demo' }
elseif ($PdfPath) { $editorArgs += (Resolve-Path -LiteralPath $PdfPath).Path }
& dotnet @editorArgs
if ($LASTEXITCODE -ne 0) { throw "Editor exited with code $LASTEXITCODE." }
