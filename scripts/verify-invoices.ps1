param([string]$OutputPath = "output/invoices", [string]$PythonPath = "python")
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
& $PythonPath (Join-Path $PSScriptRoot "verify-invoices.py") $OutputPath
if ($LASTEXITCODE -ne 0) { throw "Independent invoice verification failed." }
