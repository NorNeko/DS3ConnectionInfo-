[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'DS3ConnectionInfo\DS3ConnectionInfo.csproj'
$payloadDirectory = Join-Path $repoRoot 'artifacts\payload'
$releaseDirectory = Join-Path $repoRoot 'artifacts\release'
$releaseName = 'v4.5.0-cn.2'
$releaseFile = Join-Path $releaseDirectory "DS3ConnectionInfo-$releaseName-win-x64.exe"

function Reset-Directory([string]$path) {
    if (Test-Path -LiteralPath $path) {
        $resolved = (Resolve-Path -LiteralPath $path).Path
        $artifactsRoot = (Resolve-Path -LiteralPath (Join-Path $repoRoot 'artifacts')).Path
        if (-not $resolved.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to clear a directory outside artifacts: $resolved"
        }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
    New-Item -ItemType Directory -Path $path | Out-Null
}

New-Item -ItemType Directory -Path (Join-Path $repoRoot 'artifacts') -Force | Out-Null
Reset-Directory $payloadDirectory
Reset-Directory $releaseDirectory

function Resolve-ModernMSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $found = & $vswhere -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($found) { return $found }
    }

    $visualStudioRoot = Join-Path $env:ProgramFiles 'Microsoft Visual Studio'
    if (Test-Path -LiteralPath $visualStudioRoot) {
        $found = Get-ChildItem -LiteralPath $visualStudioRoot -Filter MSBuild.exe -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -notmatch '\\amd64\\' } |
            Select-Object -First 1 -ExpandProperty FullName
        if ($found) { return $found }
    }

    throw 'Modern MSBuild was not found. Install Visual Studio 2022 or newer with .NET desktop build tools.'
}

$msbuild = Resolve-ModernMSBuild
& $msbuild $projectPath /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU "/p:OutputPath=$payloadDirectory\"
if ($LASTEXITCODE -ne 0) { throw "Application build failed with exit code $LASTEXITCODE." }

$requiredFiles = @(
    'DS3ConnectionInfo.exe',
    'steam_api64.dll',
    'amd64\KernelTraceControl.dll',
    'amd64\msdia140.dll',
    'amd64\msvcp140.dll',
    'amd64\vcruntime140_1.dll',
    'amd64\vcruntime140.dll'
)
foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $payloadDirectory $file))) {
        throw "Release payload is missing: $file"
    }
}

$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework C# compiler was not found.' }

$compilerArguments = @(
    '/nologo',
    '/target:winexe',
    '/optimize+',
    '/platform:x64',
    "/out:$releaseFile",
    "/win32manifest:$(Join-Path $repoRoot 'packaging\launcher.manifest')",
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Windows.Forms.dll',
    "/resource:$(Join-Path $payloadDirectory 'DS3ConnectionInfo.exe'),DS3ConnectionInfo.Payload.App",
    "/resource:$(Join-Path $payloadDirectory 'steam_api64.dll'),DS3ConnectionInfo.Payload.Steam",
    "/resource:$(Join-Path $payloadDirectory 'amd64\KernelTraceControl.dll'),DS3ConnectionInfo.Payload.KernelTrace",
    "/resource:$(Join-Path $payloadDirectory 'amd64\msdia140.dll'),DS3ConnectionInfo.Payload.Msdia",
    "/resource:$(Join-Path $payloadDirectory 'amd64\msvcp140.dll'),DS3ConnectionInfo.Payload.Msvcp",
    "/resource:$(Join-Path $payloadDirectory 'amd64\vcruntime140_1.dll'),DS3ConnectionInfo.Payload.Vcruntime1",
    "/resource:$(Join-Path $payloadDirectory 'amd64\vcruntime140.dll'),DS3ConnectionInfo.Payload.Vcruntime",
    (Join-Path $repoRoot 'packaging\SingleFileLauncher.cs')
)
& $compiler $compilerArguments
if ($LASTEXITCODE -ne 0) { throw "Single-file launcher build failed with exit code $LASTEXITCODE." }

$smokeError = Join-Path $repoRoot 'single-file-smoke-error.txt'
if (Test-Path -LiteralPath $smokeError) { Remove-Item -LiteralPath $smokeError -Force }
$smokeProcess = Start-Process -FilePath $releaseFile -ArgumentList '--extract-only' -WorkingDirectory $repoRoot -PassThru
if (-not $smokeProcess.WaitForExit(30000)) {
    Stop-Process -Id $smokeProcess.Id -Force
    throw 'Single-file extraction smoke test timed out.'
}
if ($smokeProcess.ExitCode -ne 0) {
    $detail = if (Test-Path -LiteralPath $smokeError) { Get-Content -LiteralPath $smokeError -Raw } else { 'No diagnostic file was produced.' }
    throw "Single-file extraction smoke test failed with exit code $($smokeProcess.ExitCode). $detail"
}

$hash = (Get-FileHash -LiteralPath $releaseFile -Algorithm SHA256).Hash.ToLowerInvariant()
$hashFile = "$releaseFile.sha256"
Set-Content -LiteralPath $hashFile -Value "$hash  $([IO.Path]::GetFileName($releaseFile))" -Encoding ASCII

Write-Host "Release EXE: $releaseFile"
Write-Host "SHA-256:    $hash"
