# Dot-source this file before invoking a repository Docker build.
function Get-SixLaborsDockerSecretArguments {
    param([Parameter(Mandatory)][string]$Repository)

    if ($env:SIXLABORS_LICENSE_KEY) {
        return @('--secret', 'id=sixlabors_license,env=SIXLABORS_LICENSE_KEY')
    }

    $licenseFile = $env:SixLaborsLicenseFile
    if (-not $licenseFile) {
        $project = Join-Path $Repository 'src/ZingPDF/ZingPDF.csproj'
        $licenseFile = & dotnet msbuild $project -nologo -getProperty:SixLaborsLicenseFile
        if ($LASTEXITCODE -ne 0) { throw 'Could not resolve the local SixLabors licence file.' }
        $licenseFile = ($licenseFile -join '').Trim()
    }

    if (-not $licenseFile -or -not (Test-Path -LiteralPath $licenseFile -PathType Leaf)) {
        throw 'Set SIXLABORS_LICENSE_KEY or configure SixLaborsLicenseFile before building the Linux corpus image.'
    }
    return @('--secret', "id=sixlabors_license,src=$licenseFile")
}
