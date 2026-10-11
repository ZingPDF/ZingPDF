param(
    [string]$Image = 'zingpdf-invoice-smoke:local',
    [string]$ChromiumVersion = '',
    [string]$SdkImage = 'mcr.microsoft.com/dotnet/sdk:10.0-noble',
    [string]$RuntimeImage = 'mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim',
    [string]$Python = 'python'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
foreach ($command in @('docker', $Python)) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "Required command not found: $command" }
}
$repository = Split-Path -Parent $PSScriptRoot
$runId = [guid]::NewGuid().ToString('N')
$container = "zingpdf-invoices-$runId"
$volume = "zingpdf-invoices-$runId"
$output = Join-Path $repository ".tools/invoice-linux/$runId"
$volumeCreated = $false
$containerCreated = $false
function Invoke-Docker {
    param([string[]]$DockerArguments)
    & docker @DockerArguments
    if ($LASTEXITCODE -ne 0) { throw "Docker failed: $($DockerArguments -join ' ')" }
}
try {
    Invoke-Docker -DockerArguments @('build', '-f', (Join-Path $repository 'examples/GenerateInvoices/Dockerfile'), '-t', $Image,
        '--build-arg', "CHROMIUM_VERSION=$ChromiumVersion", '--build-arg', "SDK_IMAGE=$SdkImage",
        '--build-arg', "RUNTIME_IMAGE=$RuntimeImage", $repository)
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    Invoke-Docker -DockerArguments @('volume', 'create', $volume)
    $volumeCreated = $true
    Invoke-Docker -DockerArguments @('create', '--name', $container, '--network', 'none', '--read-only', '--cap-drop', 'ALL',
        '--security-opt', 'no-new-privileges', '--tmpfs', '/tmp:rw,nosuid,nodev,size=512m',
        '--shm-size', '256m', '--memory', '2g', '--cpus', '2', '--pids-limit', '256',
        '--mount', "type=volume,source=$volume,target=/out", '--env', 'ZINGPDF_NO_SANDBOX=1', $Image, '/out')
    $containerCreated = $true
    & docker image inspect $Image | Set-Content -LiteralPath (Join-Path $output 'image-inspect.json')
    if ($LASTEXITCODE -ne 0) { throw 'Could not record the image identity.' }
    # Copy output even when the worker fails, to retain the partial corpus for diagnosis.
    & docker start -a $container
    $runExit = $LASTEXITCODE
    Invoke-Docker -DockerArguments @('cp', "${container}:/out/.", $output)
    Invoke-Docker -DockerArguments @('cp', "${container}:/app/browser-packages.txt", (Join-Path $output 'browser-packages.txt'))
    $containerState = & docker inspect $container
    if ($LASTEXITCODE -ne 0) { throw 'Could not record the container configuration.' }
    $containerState | Set-Content -LiteralPath (Join-Path $output 'container-inspect.json')
    if ($runExit -ne 0) { throw "Invoice worker failed with exit code $runExit. Partial outputs: $output" }
    $workerExit = ($containerState -join "`n" | ConvertFrom-Json)[0].State.ExitCode
    if ($workerExit -ne 0) { throw "Invoice worker failed with exit code $workerExit. Partial outputs: $output" }
    & $Python (Join-Path $PSScriptRoot 'verify-invoices.py') $output 2>&1 |
        Tee-Object -FilePath (Join-Path $output 'verification.txt')
    if ($LASTEXITCODE -ne 0) { throw "Independent PDF verification failed. Outputs: $output" }
    Write-Host "Verified Linux invoice corpus: $output"
}
finally {
    # Only remove the uniquely named resources successfully created by this invocation.
    if ($containerCreated) {
        & docker rm -f $container | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not remove container $container" }
    }
    if ($volumeCreated) {
        & docker volume rm $volume | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not remove volume $volume" }
    }
}
