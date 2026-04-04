param()

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$excludedNames = @(
  "README.md",
  "commercial-terms.html",
  "eula.html",
  "evaluation.html",
  "license.html",
  "privacy.html",
  "refund-policy.html",
  "support-policy.html"
)

$files = @()
$files += Get-ChildItem -Path $root -File -Include *.html,*.js,*.md
$files += Get-ChildItem -Path (Join-Path $root "api-src") -File -Filter *.md -ErrorAction SilentlyContinue
$files = $files |
  Where-Object { $excludedNames -notcontains $_.Name } |
  Sort-Object FullName -Unique

$rules = @(
  @{
    Name = "Owner-facing site copy"
    Pattern = "blog engine|CMS|static pages?|content strategy|how the site is maintained|the site itself"
  },
  @{
    Name = "Audience-observer phrasing"
    Pattern = "people actually search for|trying to get a feature working|buyers usually ask|teams care about|what teams evaluate first|trip people up"
  },
  @{
    Name = "Meta-writing filler"
    Pattern = "the useful part is|worth knowing|what this means is|the nice thing is|the point is"
  },
  @{
    Name = "Weak marketing adjectives"
    Pattern = "\b(practical|straightforward|robust|powerful|flexible|helpful|seamless|focused)\b"
  }
)

$findings = @()

foreach ($file in $files) {
  foreach ($rule in $rules) {
    $matches = Select-String -Path $file.FullName -Pattern $rule.Pattern -CaseSensitive:$false
    foreach ($match in $matches) {
      $findings += [PSCustomObject]@{
        Rule = $rule.Name
        Path = $file.FullName
        Line = $match.LineNumber
        Text = $match.Line.Trim()
      }
    }
  }
}

if ($findings.Count -gt 0) {
  Write-Host "Copy check failed:`n" -ForegroundColor Red
  foreach ($finding in $findings) {
    Write-Host "[$($finding.Rule)] $($finding.Path):$($finding.Line)" -ForegroundColor Yellow
    Write-Host "  $($finding.Text)"
  }
  exit 1
}

Write-Host "Copy check passed." -ForegroundColor Green
