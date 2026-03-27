param(
    [string]$PropsPath = "Directory.Build.props",
    [string]$ChangelogPath = "CHANGELOG.md",
    [string]$OverrideVersion = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $repoRoot

function Get-LatestVersion {
    param([string]$FallbackVersion)

    $tags = @(git tag --list "v*" --sort=-v:refname)
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read git tags."
    }

    $tag = $tags | Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($tag)) {
        return $FallbackVersion
    }

    $version = $tag.Trim().TrimStart("v")
    if ($version -notmatch '^\d+\.\d+\.\d+$') {
        throw "Latest tag '$tag' is not a supported release tag."
    }

    return $version
}

function Get-LatestTag {
    $tags = @(git tag --list "v*" --sort=-v:refname)
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read git tags."
    }

    return $tags | Select-Object -First 1
}

function Get-NextPatchVersion {
    param([string]$Version)

    $parts = $Version.Split(".")
    if ($parts.Length -ne 3) {
        throw "Version '$Version' is not in major.minor.patch format."
    }

    return "{0}.{1}.{2}" -f [int]$parts[0], [int]$parts[1], ([int]$parts[2] + 1)
}

function Test-StableSemVer {
    param([string]$Version)

    return $Version -match '^\d+\.\d+\.\d+$'
}

function Compare-SemVer {
    param(
        [string]$Left,
        [string]$Right
    )

    $leftVersion = [version]$Left
    $rightVersion = [version]$Right

    return $leftVersion.CompareTo($rightVersion)
}

function Get-CommitNotes {
    param([string]$BaseTag)

    $range = if ([string]::IsNullOrWhiteSpace($BaseTag)) { "HEAD" } else { "$BaseTag..HEAD" }
    $subjects = git log $range --pretty=format:%s
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read git history for changelog generation."
    }

    $notes = @()
    foreach ($subject in $subjects) {
        if ([string]::IsNullOrWhiteSpace($subject)) {
            continue
        }

        if ($subject -match '^\s*chore\(release\):') {
            continue
        }

        $notes += "- $subject.Trim()"
    }

    return $notes
}

function Get-UnreleasedBounds {
    param([string[]]$Lines)

    $startIndex = [Array]::IndexOf($Lines, "## [Unreleased]")
    if ($startIndex -lt 0) {
        throw "CHANGELOG.md must contain a '## [Unreleased]' heading."
    }

    $endIndex = $Lines.Length
    for ($i = $startIndex + 1; $i -lt $Lines.Length; $i++) {
        if ($Lines[$i] -match '^## \[') {
            $endIndex = $i
            break
        }
    }

    return @{
        Start = $startIndex
        End = $endIndex
    }
}

$propsContent = Get-Content $PropsPath -Raw
$versionMatch = [regex]::Match($propsContent, '<VersionBase>(?<version>\d+\.\d+\.\d+)</VersionBase>')

if (-not $versionMatch.Success) {
    throw "Directory.Build.props must define VersionBase."
}

$fallbackVersion = $versionMatch.Groups['version'].Value
if (-not (Test-StableSemVer -Version $fallbackVersion)) {
    throw "VersionBase '$fallbackVersion' must be major.minor.patch."
}

$latestTag = Get-LatestTag
$tagVersion = Get-LatestVersion -FallbackVersion $fallbackVersion
$latestVersion = if ((Compare-SemVer -Left $tagVersion -Right $fallbackVersion) -ge 0) {
    $tagVersion
}
else {
    $fallbackVersion
}
$latestTag = if ((Compare-SemVer -Left $tagVersion -Right $fallbackVersion) -ge 0) { $latestTag } else { "" }
$nextVersion = Get-NextPatchVersion -Version $latestVersion

if (-not [string]::IsNullOrWhiteSpace($OverrideVersion)) {
    $OverrideVersion = $OverrideVersion.Trim()
    if (-not (Test-StableSemVer -Version $OverrideVersion)) {
        throw "OverrideVersion '$OverrideVersion' must be a stable SemVer string like 2.0.0."
    }

    if ((Compare-SemVer -Left $OverrideVersion -Right $latestVersion) -le 0) {
        throw "OverrideVersion '$OverrideVersion' must be greater than the latest release version '$latestVersion'."
    }

    $nextVersion = $OverrideVersion
}

$existingReleaseTag = git tag --list "v$nextVersion"
if ($LASTEXITCODE -ne 0) {
    throw "Unable to verify whether release tag v$nextVersion already exists."
}
if (-not [string]::IsNullOrWhiteSpace(($existingReleaseTag | Select-Object -First 1))) {
    throw "Release tag v$nextVersion already exists."
}

$releaseDate = (Get-Date).ToString("yyyy-MM-dd")

$lines = [System.IO.File]::ReadAllLines((Resolve-Path $ChangelogPath))
$bounds = Get-UnreleasedBounds -Lines $lines

$unreleasedBody = @()
if ($bounds.End -gt ($bounds.Start + 1)) {
    $unreleasedBody = $lines[($bounds.Start + 1)..($bounds.End - 1)]
}

$bodyHasContent = ($unreleasedBody | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count -gt 0
if (-not $bodyHasContent) {
    $commitNotes = Get-CommitNotes -BaseTag $latestTag
    if ($commitNotes.Count -eq 0) {
        "should_release=false" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
        exit 0
    }

    $unreleasedBody = @(
        "",
        "### Changed",
        ""
    ) + $commitNotes + @("")
}

$before = if ($bounds.Start -gt 0) { $lines[0..$bounds.Start] } else { @($lines[0]) }
$after = if ($bounds.End -lt $lines.Length) { $lines[$bounds.End..($lines.Length - 1)] } else { @() }

$normalizedBody = @($unreleasedBody)
while ($normalizedBody.Count -gt 0 -and [string]::IsNullOrWhiteSpace($normalizedBody[0])) {
    $normalizedBody = if ($normalizedBody.Count -gt 1) { $normalizedBody[1..($normalizedBody.Count - 1)] } else { @() }
}
while ($normalizedBody.Count -gt 0 -and [string]::IsNullOrWhiteSpace($normalizedBody[$normalizedBody.Count - 1])) {
    $normalizedBody = if ($normalizedBody.Count -gt 1) { $normalizedBody[0..($normalizedBody.Count - 2)] } else { @() }
}

$newLines = @()
$newLines += $before
$newLines += ""
$newLines += "## [$nextVersion] - $releaseDate"
$newLines += ""
$newLines += $normalizedBody
$newLines += ""
$newLines += $after

[System.IO.File]::WriteAllLines((Resolve-Path $ChangelogPath), $newLines)

$updatedProps = [regex]::Replace(
    $propsContent,
    '<VersionBase>\d+\.\d+\.\d+</VersionBase>',
    "<VersionBase>$nextVersion</VersionBase>",
    1
)
[System.IO.File]::WriteAllText((Resolve-Path $PropsPath), $updatedProps)

"should_release=true" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
"version=$nextVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
"assembly_version=$nextVersion.0" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
"tag=v$nextVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append

Pop-Location
