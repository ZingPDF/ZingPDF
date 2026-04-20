Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$port = 8080

Write-Host "Serving website from $scriptRoot"
Write-Host "Site: http://localhost:$port/"
Write-Host "API reference: http://localhost:$port/api/"
Push-Location $scriptRoot
try {
    python -m http.server $port
}
finally {
    Pop-Location
}
