param(
    [string]$GameDir = $env:DSP_DIR,
    [string]$ProfileDir = '',
    [switch]$Install
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    throw "csc.exe not found at $csc"
}

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    throw "GameDir is empty. Set the DSP_DIR environment variable or pass -GameDir `"<game folder>`"."
}

$assembly = Join-Path $GameDir 'DSPGAME_Data\Managed\Assembly-CSharp.dll'
if (-not (Test-Path $assembly)) {
    throw "Assembly-CSharp.dll not found under $GameDir. Check the game folder or set DSP_DIR."
}

$managed = Join-Path $GameDir 'DSPGAME_Data\Managed'
$core = Join-Path $GameDir 'BepInEx\core'
$outDir = Join-Path $root 'bin'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$netFx = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$refs = @(
    (Join-Path $core 'BepInEx.dll'),
    (Join-Path $core '0Harmony.dll'),
    (Join-Path $managed 'Assembly-CSharp.dll'),
    (Join-Path $managed 'UnityEngine.dll'),
    (Join-Path $managed 'UnityEngine.CoreModule.dll'),
    (Join-Path $managed 'UnityEngine.UI.dll'),
    (Join-Path $managed 'UnityEngine.InputLegacyModule.dll'),
    (Join-Path $managed 'UnityEngine.UIModule.dll'),
    (Join-Path $managed 'UnityEngine.TextRenderingModule.dll'),
    (Join-Path $managed 'netstandard.dll'),
    (Join-Path $netFx 'System.dll'),
    (Join-Path $netFx 'System.Core.dll')
)

$refArgs = $refs | ForEach-Object { '/reference:' + $_ }
$source = Join-Path $root 'src\MaxStationPlugin.cs'
$outDll = Join-Path $outDir 'MaxStation.dll'

& $csc /nologo /target:library /langversion:5 /codepage:65001 "/out:$outDll" $refArgs $source
if ($LASTEXITCODE -ne 0) {
    throw "csc failed with exit code $LASTEXITCODE"
}

Write-Host "Built: $outDll"

if ($Install) {
    if ([string]::IsNullOrWhiteSpace($ProfileDir)) {
        $pluginDir = Join-Path $GameDir 'BepInEx\plugins\MaxStation'
    }
    else {
        $pluginDir = Join-Path $ProfileDir 'BepInEx\plugins\MaxStation'
    }

    New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
    Copy-Item $outDll $pluginDir -Force
    Write-Host "Installed: $pluginDir\MaxStation.dll"
}
