param(
    [string]$PackageVersion = "1.0.59"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$runRoot = Join-Path $repoRoot (".tools/runtime-consumers/" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null

# Use a real, complete public-package consumer; never substitute project references.
Expand-Archive -LiteralPath (Join-Path $repoRoot "website/assets/examples/first-pdf.zip") -DestinationPath $runRoot
$packageIds = @("ZingPDF", "ZingPDF.FromHTML", "ZingPDF.GoogleFonts", "ZingPDF.OCR", "ZingPDF.Templates", "ZingPDF.Templates.LiquidHtml")
$references = ($packageIds | ForEach-Object { "    <PackageReference Include=`"$_`" Version=`"$PackageVersion`" />" }) -join "`n"
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
$references
  </ItemGroup>
</Project>
"@
Set-Content -LiteralPath (Join-Path $runRoot "FirstPdf.csproj") -Value $project -Encoding utf8
$programPath = Join-Path $runRoot "Program.cs"
$program = Get-Content -LiteralPath $programPath -Raw
$runtimeCheck = @'
var requiredRuntimeMajor = int.Parse(args.Single());
if (Environment.Version.Major != requiredRuntimeMajor)
    throw new InvalidOperationException($"Expected runtime {requiredRuntimeMajor}, got {Environment.Version}.");
Console.WriteLine($"RUNTIME: {Environment.Version}");
foreach (var runtimePackage in new[] { "ZingPDF", "ZingPDF.FromHTML", "ZingPDF.GoogleFonts", "ZingPDF.OCR", "ZingPDF.Templates", "ZingPDF.Templates.LiquidHtml" })
{
    var runtimeAssembly = System.Reflection.Assembly.Load(runtimePackage);
    Console.WriteLine($"ASSEMBLY: {runtimeAssembly.GetName().Name} {runtimeAssembly.GetName().Version}");
}

'@
$program = $program.Replace('Directory.CreateDirectory("output");', $runtimeCheck + "`n" + 'Directory.CreateDirectory("output");')
Set-Content -LiteralPath $programPath -Value $program -Encoding utf8

$results = @()
foreach ($major in @(8, 9, 10)) {
    $framework = "net$major.0"
    Push-Location $runRoot
    try {
        $runOutput = & dotnet run --project FirstPdf.csproj --framework $framework --configuration Release -- $major 2>&1
        $exitCode = $LASTEXITCODE
        $runOutput | Set-Content -LiteralPath "$framework.log" -Encoding utf8
        if ($exitCode -ne 0) {
            $runOutput | Write-Host
            throw "Published-package consumer failed on $framework."
        }
        $lines = @($runOutput | ForEach-Object { $_.ToString() })
        $runtimeLine = @($lines | Where-Object { $_.StartsWith("RUNTIME: ") })
        $assemblies = @($lines | Where-Object { $_.StartsWith("ASSEMBLY: ") })
        if ($runtimeLine.Count -ne 1 -or $assemblies.Count -ne 6 -or
            -not ($lines | Where-Object { $_.StartsWith("Verified: 2 pages;") })) {
            throw "Consumer evidence is incomplete on $framework."
        }
        $resultRoot = Join-Path $runRoot $framework
        New-Item -ItemType Directory -Path $resultRoot -Force | Out-Null
        Copy-Item -Path (Join-Path $runRoot "output/*") -Destination $resultRoot
        $results += [ordered]@{
            framework = $framework
            runtime = $runtimeLine[0].Substring("RUNTIME: ".Length)
            assemblies = $assemblies
            pageAndTextAssertionsPassed = $true
        }
        Write-Host "${framework}: $($runtimeLine[0]); six package assemblies loaded; create/edit/save/reopen/text checks passed."
    }
    finally { Pop-Location }
}

[ordered]@{
    packageVersion = $PackageVersion
    os = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
    results = $results
    scope = "Loads all six package assemblies and checks core PDF authoring, editing, save/reopen, page counts, and embedded text. Native HTML/OCR deployment requirements are separate."
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $runRoot "results.json") -Encoding utf8
Write-Host "Consumer evidence: $runRoot"
