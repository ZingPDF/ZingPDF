Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$targets = @(
    "TestFiles/pdf/*.pdf",
    "TestFiles/image/*"
)

$files = @(git ls-files -- $targets)
if ($LASTEXITCODE -ne 0) {
    throw "Unable to enumerate binary fixtures."
}

$bad = New-Object System.Collections.Generic.List[string]

foreach ($file in $files) {
    $attrs = @(git check-attr text eol -- $file)
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to inspect Git attributes for $file."
    }

    $textAttr = $attrs | Where-Object { $_ -match ':\s+text:\s+' }
    $eolAttr = $attrs | Where-Object { $_ -match ':\s+eol:\s+' }

    if ($textAttr -match ':\s+text:\s+(set|auto)$') {
        $bad.Add("$file is not marked as binary in Git attributes: $textAttr")
        continue
    }

    if ($eolAttr -match ':\s+eol:\s+(lf|crlf)$') {
        $bad.Add("$file has an explicit text EOL policy applied unexpectedly: $eolAttr")
    }
}

if ($bad.Count -gt 0) {
    $details = ($bad -join [Environment]::NewLine)
    throw "Binary fixture Git attributes are incorrect:`n$details"
}

Write-Host "Binary fixture Git attributes look correct."
