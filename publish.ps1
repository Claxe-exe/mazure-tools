<#
.SYNOPSIS
  Builds a portable Mazure Tools that can be copied to any Windows 10/11 PC.

.DESCRIPTION
  Requires the .NET SDK on THIS machine only (winget install Microsoft.DotNet.SDK.10).
  The result is a single MazureTools.exe plus a zip, written to .\dist

  SelfContained       (default) Bundles the .NET runtime. Runs on a clean PC with nothing installed.
                      ~170 MB exe, ~66 MB when zipped.
  FrameworkDependent  Tiny (a few MB) but the target PC needs the ".NET Desktop Runtime 10" installed.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\publish.ps1
  powershell -ExecutionPolicy Bypass -File .\publish.ps1 -Mode FrameworkDependent
  powershell -ExecutionPolicy Bypass -File .\publish.ps1 -Runtime win-arm64
#>
[CmdletBinding()]
param(
    [ValidateSet('SelfContained', 'FrameworkDependent')]
    [string]$Mode = 'SelfContained',

    [ValidateSet('win-x64', 'win-arm64', 'win-x86')]
    [string]$Runtime = 'win-x64',

    [switch]$NoZip
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK was not found. Install it with:  winget install Microsoft.DotNet.SDK.10'
}

$suffix = if ($Mode -eq 'SelfContained') { $Runtime } else { "$Runtime-needs-dotnet" }
$out = Join-Path $PSScriptRoot "dist\MazureTools-$suffix"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }

$selfContained = $Mode -eq 'SelfContained'
$args = @(
    'publish', 'MazureTools.csproj',
    '-c', 'Release',
    '-r', $Runtime,
    '--self-contained', $selfContained.ToString().ToLower(),
    '-p:PublishSingleFile=true',
    '-p:DebugType=None',
    '-p:DebugSymbols=false',
    '-o', $out
)
if ($selfContained) {
    # WPF's native libraries are packed inside the exe and unpacked on first start.
    # Compression is deliberately OFF: a compressed single file has to be inflated into RAM on every start
    # (~+80 MB private memory). The zip compresses just as well and costs nothing at run time.
    $args += '-p:IncludeNativeLibrariesForSelfExtract=true'
    $args += '-p:EnableCompressionInSingleFile=false'
}

Write-Host "Publishing ($Mode, $Runtime)..." -ForegroundColor Cyan
& dotnet @args
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$exe = Join-Path $out 'MazureTools.exe'
$sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host "Built $exe ($sizeMb MB)" -ForegroundColor Green

if (-not $NoZip) {
    $zip = "$out.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip
    Write-Host "Zip:   $zip ($([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB)" -ForegroundColor Green
}

Write-Host "Copy the exe (or the zip) to the other PC and double-click MazureTools.exe." -ForegroundColor Cyan
