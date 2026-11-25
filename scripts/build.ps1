<#
.SYNOPSIS
  Builds DeskDot locally.

.DESCRIPTION
  Restores and builds the WPF solution for the requested architecture.
  Pass -Publish to also produce a self-contained, single-folder build in publish\<rid>,
  and -Installer to compile the Inno Setup installer from it (requires ISCC.exe on PATH
  or the default Inno Setup 6 install location).

.EXAMPLE
  pwsh ./scripts/build.ps1 -Configuration Release -Publish
.EXAMPLE
  pwsh ./scripts/build.ps1 -Configuration Release -Publish -Installer
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime = 'win-x64',

    [switch]$Publish,
    [switch]$Installer,
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src/DeskDot/DeskDot.csproj'
$solution = Join-Path $root 'DeskDot.sln'

function Invoke-Step {
    param([string]$Label, [scriptblock]$Action)

    Write-Host "==> $Label" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "$Label failed with exit code $LASTEXITCODE"
    }
}

Invoke-Step 'Restore' { dotnet restore $solution }
Invoke-Step "Build ($Configuration)" {
    dotnet build $solution -c $Configuration --no-restore `
        -p:Platform=x64 -p:Version=$Version -p:FileVersion="$Version.0"
}

if ($Publish) {
    $out = Join-Path $root "publish/$Runtime"
    if (Test-Path $out) { Remove-Item $out -Recurse -Force }

    Invoke-Step "Publish self-contained ($Runtime)" {
        dotnet publish $project -c $Configuration -r $Runtime --self-contained true `
            -p:PublishSingleFile=false -p:Version=$Version -p:FileVersion="$Version.0" `
            -o $out
    }

    Write-Host "Published to $out" -ForegroundColor Green
}

if ($Installer) {
    $iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if (-not $iscc) {
        $candidate = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
        if (Test-Path $candidate) { $iscc = Get-Item $candidate } else {
            throw 'Inno Setup 6 not found. Install it or add ISCC.exe to PATH.'
        }
    }

    Invoke-Step 'Compile installer' {
        & $iscc.Source (Join-Path $root 'installer/DeskDot.iss') "/DAppVersion=$Version"
    }

    Write-Host "Installer written to $(Join-Path $root 'dist')" -ForegroundColor Green
}
