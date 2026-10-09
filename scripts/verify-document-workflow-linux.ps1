param(
    [string]$Image = 'zingpdf-document-workflow:local',
    [string]$TesseractVersion = '',
    [string]$EngDataVersion = '',
    [string]$OsdDataVersion = '',
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
$container = "zingpdf-document-workflow-$runId"
$supervisorContainer = "zingpdf-document-workflow-stall-$runId"
$volume = "zingpdf-document-workflow-$runId"
$output = Join-Path $repository ".tools/document-workflow-linux/$runId"
$volumeCreated = $false
$containerCreated = $false
$supervisorContainerCreated = $false
function Invoke-Docker {
    param([string[]]$DockerArguments)
    & docker @DockerArguments
    if ($LASTEXITCODE -ne 0) { throw "Docker failed: $($DockerArguments -join ' ')" }
}
try {
    Invoke-Docker -DockerArguments @('build', '-f', (Join-Path $repository 'examples/VerifyDocumentWorkflow/Dockerfile'), '-t', $Image,
        '--build-arg', "TESSERACT_VERSION=$TesseractVersion", '--build-arg', "ENG_DATA_VERSION=$EngDataVersion",
        '--build-arg', "OSD_DATA_VERSION=$OsdDataVersion",
        '--build-arg', "SDK_IMAGE=$SdkImage", '--build-arg', "RUNTIME_IMAGE=$RuntimeImage", $repository)
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    Invoke-Docker -DockerArguments @('volume', 'create', $volume)
    $volumeCreated = $true
    Invoke-Docker -DockerArguments @('create', '--name', $container, '--network', 'none', '--read-only', '--cap-drop', 'ALL',
        '--security-opt', 'no-new-privileges', '--stop-timeout', '5', '--tmpfs', '/tmp:rw,nosuid,nodev,size=512m',
        '--shm-size', '256m', '--memory', '8g', '--cpus', '4', '--pids-limit', '256',
        '--mount', "type=volume,source=$volume,target=/out", $Image, '/out')
    $containerCreated = $true
    & docker image inspect $Image | Set-Content -LiteralPath (Join-Path $output 'image-inspect.json')
    if ($LASTEXITCODE -ne 0) { throw 'Could not record the image identity.' }
    # Copy the corpus and partial evidence even when the worker deadline or OCR process fails.
    & docker start -a $container
    $runExit = $LASTEXITCODE
    Invoke-Docker -DockerArguments @('cp', "${container}:/out/.", $output)
    foreach ($evidenceFile in @('ocr-packages.tsv', 'tessdata.sha256', 'pdfium.sha256', 'tesseract-version.txt', 'tesseract-languages.txt')) {
        Invoke-Docker -DockerArguments @('cp', "${container}:/app/$evidenceFile", (Join-Path $output $evidenceFile))
    }
    $containerState = & docker inspect $container
    if ($LASTEXITCODE -ne 0) { throw 'Could not record the container configuration.' }
    $containerState | Set-Content -LiteralPath (Join-Path $output 'container-inspect.json')
    if ($runExit -ne 0) { throw "OCR worker failed with exit code $runExit. Partial outputs: $output" }
    $workerExit = ($containerState -join "`n" | ConvertFrom-Json)[0].State.ExitCode
    if ($workerExit -ne 0) { throw "OCR worker failed with exit code $workerExit. Partial outputs: $output" }

    # Prove the image's same GNU timeout supervisor terminates a deliberately stalled worker.
    $supervisorTimer = [System.Diagnostics.Stopwatch]::StartNew()
    Invoke-Docker -DockerArguments @('create', '--name', $supervisorContainer, '--network', 'none', '--read-only', '--cap-drop', 'ALL',
        '--security-opt', 'no-new-privileges', '--stop-timeout', '2', '--tmpfs', '/tmp:rw,nosuid,nodev,size=512m',
        '--shm-size', '256m', '--memory', '8g', '--cpus', '4', '--pids-limit', '256',
        '--entrypoint', '/usr/bin/timeout', $Image, '--signal=TERM', '--kill-after=1s', '3s', 'dotnet', 'VerifyDocumentWorkflow.dll', '--simulate-stall')
    $supervisorContainerCreated = $true
    & docker start -a $supervisorContainer | Set-Content -LiteralPath (Join-Path $output 'supervisor-stall-output.txt')
    $supervisorRunExit = $LASTEXITCODE
    $supervisorTimer.Stop()
    $supervisorState = & docker inspect $supervisorContainer
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect the supervisor probe container.' }
    $supervisorState | Set-Content -LiteralPath (Join-Path $output 'supervisor-container-inspect.json')
    $supervisorExit = ($supervisorState -join "`n" | ConvertFrom-Json)[0].State.ExitCode
    if ($supervisorRunExit -ne 124 -or $supervisorExit -ne 124 -or $supervisorTimer.Elapsed.TotalSeconds -ge 20) {
        throw "Worker supervisor probe failed (attach=$supervisorRunExit, container=$supervisorExit, elapsed=$([math]::Round($supervisorTimer.Elapsed.TotalSeconds, 2))s)."
    }
    [ordered]@{ ConfiguredWorkerDeadlineSeconds = 60; ProbeDeadlineSeconds = 3; ExpectedTimeoutExitCode = 124;
        AttachExitCode = $supervisorRunExit; ContainerExitCode = $supervisorExit; ElapsedMilliseconds = $supervisorTimer.ElapsedMilliseconds } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'supervisor-check.json')

    & $Python (Join-Path $PSScriptRoot 'verify-document-workflow.py') $output 2>&1 |
        Tee-Object -FilePath (Join-Path $output 'verification.txt')
    if ($LASTEXITCODE -ne 0) { throw "Independent OCR verification failed. Outputs: $output" }
    Write-Host "Verified Linux document workflow corpus: $output"
}
finally {
    # Only remove the unique resources successfully created by this invocation.
    if ($containerCreated) {
        & docker rm -f $container | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not remove container $container" }
    }
    if ($supervisorContainerCreated) {
        & docker rm -f $supervisorContainer | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not remove supervisor probe container $supervisorContainer" }
    }
    if ($volumeCreated) {
        & docker volume rm $volume | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not remove volume $volume" }
    }
}
