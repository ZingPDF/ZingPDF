Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$targets = @(
    "TestFiles/pdf/*.pdf",
    "TestFiles/image/*"
)

$output = git ls-files --eol -- $targets
if ($LASTEXITCODE -ne 0) {
    throw "Unable to inspect Git EOL state for binary fixtures."
}

$bad = @($output | Where-Object { $_ -match '\bw/(crlf|mixed)\b' })
if ($bad.Count -gt 0) {
    $details = ($bad -join [Environment]::NewLine)
    throw "Binary fixtures were checked out with text line endings:`n$details"
}

Write-Host "Binary fixture checkout looks correct."
