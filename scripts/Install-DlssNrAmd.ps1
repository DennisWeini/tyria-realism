param(
    [string]$DllPath = '',
    [switch]$DownloadModelFromRhi,
    [ValidateSet('Quality', 'Balanced', 'Cinematic', 'RSR1080p')][string]$Preset = 'Balanced',
    [string]$GameExe = '',
    [string]$PackageZip = ''
)

$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'Common.ps1')
$downloadUrl = 'https://github.com/mochizuki0323/DLSSNR-AMD/releases/download/v0.0.6-win-preview-alpha/DLSSNR-AMD-Vulkan-Windows-0.0.6-win-preview-alpha-hotfix-x86_64.zip'
$packageSha = 'A5687DC3875BE4BDB8149CA68BB5E3F3023402C720BF4A80A03AC2A19C436E2B'
$acceptedDlls = @{
    'E16BCF15E16E13F527491CDF7845B2FE6521A738D8F7C9C721866A8496E1FC8E' = '310.8.0'
    '2BC19BC09A7B89DD44F73E2AC0108FCC586E2154BDD0830F242F4F040E28789F' = '310.9.16'
}

function Stop-WithMessage([string]$Message) {
    Write-Host "ABBRUCH: $Message" -ForegroundColor Red
    exit 1
}

try { $GameExe = Resolve-Gw2Exe $GameExe } catch { Stop-WithMessage $_.Exception.Message }

if ($DownloadModelFromRhi) {
    if ($DllPath) { Stop-WithMessage 'Entweder eigene DLL oder RHI-Download auswaehlen, nicht beides.' }
    if (Get-Process -Name 'Gw2-64' -ErrorAction SilentlyContinue) { Stop-WithMessage 'GW2 vor der Installation beenden.' }
    try { $DllPath = & (Join-Path $PSScriptRoot 'Get-NvidiaNrDll.ps1') } catch { Stop-WithMessage $_.Exception.Message }
}

if (-not $DllPath) {
    Add-Type -AssemblyName System.Windows.Forms
    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Title = 'Unveränderte nvngx_dlssnr.dll 310.9.16 oder 310.8.0 auswählen'
    $dialog.Filter = 'NVIDIA Neural Rendering (nvngx_dlssnr.dll)|nvngx_dlssnr.dll'
    if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
        Stop-WithMessage 'Keine DLL ausgewählt. Es wurde nichts verändert.'
    }
    $DllPath = $dialog.FileName
}

if (-not (Test-Path -LiteralPath $DllPath -PathType Leaf)) {
    Stop-WithMessage "DLL wurde nicht gefunden: $DllPath"
}

$dllHash = (Get-FileHash -LiteralPath $DllPath -Algorithm SHA256).Hash
if (-not $acceptedDlls.ContainsKey($dllHash)) {
    Stop-WithMessage "Die DLL hat keinen akzeptierten Original-Hash ($dllHash). Es wurde nichts installiert."
}
$signature = Get-AuthenticodeSignature -LiteralPath $DllPath
if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or
    -not $signature.SignerCertificate -or $signature.SignerCertificate.Subject -notmatch 'NVIDIA Corporation') {
    Stop-WithMessage "Die NVIDIA-Signatur ist nicht gültig ($($signature.Status)). Es wurde nichts installiert."
}

if (Get-Process -Name 'Gw2-64' -ErrorAction SilentlyContinue) {
    Stop-WithMessage 'Guild Wars 2 läuft noch. Bitte normal beenden und den Installer danach erneut starten.'
}

$gameDir = Split-Path -Parent (Resolve-Path -LiteralPath $GameExe).Path
$manifest = Join-Path $gameDir 'dlssnr-amd-install.txt'
$existingProxy = Join-Path $gameDir 'dxgi.dll'
if ((Test-Path -LiteralPath $existingProxy) -and -not (Test-Path -LiteralPath $manifest)) {
    Stop-WithMessage "Im GW2-Ordner liegt bereits eine fremde dxgi.dll. Sie wurde nicht angerührt: $existingProxy"
}

$runtime = Join-Path $project 'runtime\DLSSNR-AMD'
$downloads = Join-Path $project 'runtime\downloads'
New-Item -ItemType Directory -Force -Path $downloads | Out-Null
if (-not $PackageZip) {
    $PackageZip = Join-Path $downloads 'DLSSNR-AMD-0.0.6-hotfix.zip'
    if (-not (Test-Path -LiteralPath $PackageZip)) {
        Write-Host 'Lade das festgelegte offizielle DLSSNR-AMD-Hotfix von GitHub ...'
        Invoke-WebRequest -Uri $downloadUrl -OutFile $PackageZip -UseBasicParsing
    }
}
if (-not (Test-Path -LiteralPath $PackageZip -PathType Leaf)) {
    Stop-WithMessage "Paket wurde nicht gefunden: $PackageZip"
}
$actualPackageSha = (Get-FileHash -LiteralPath $PackageZip -Algorithm SHA256).Hash
if ($actualPackageSha -ne $packageSha) {
    Stop-WithMessage "Paket-Hash stimmt nicht ($actualPackageSha). Es wurde nichts entpackt."
}

if (-not (Test-Path -LiteralPath (Join-Path $runtime 'install.ps1'))) {
    if (Test-Path -LiteralPath $runtime) {
        Stop-WithMessage "Unvollständiger Runtime-Ordner vorhanden. Bitte manuell prüfen: $runtime"
    }
    New-Item -ItemType Directory -Path $runtime | Out-Null
    Expand-Archive -LiteralPath $PackageZip -DestinationPath $runtime
}

$installer = Join-Path $runtime 'install.ps1'
$modelChoice = if ($acceptedDlls[$dllHash] -eq '310.9.16') { 'new' } else { 'old' }
Write-Host "Installiere ReShade-Feeder mit NVIDIA-Modell $($acceptedDlls[$dllHash]) und sicherem Standardnetz ..."
$installerArgs = @(
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $installer,
    '-Target', $GameExe, '-Route', 'reshade-feeder', '-Dll', $DllPath,
    '-Model', $modelChoice, '-Network', 'default', '-RenoDX', 'on'
)
& powershell.exe @installerArgs
$installerExit = $LASTEXITCODE
if ($installerExit -ne 0) {
    Stop-WithMessage "Der offizielle Paket-Installer meldete Code $installerExit."
}

$ini = Join-Path $gameDir 'dlssnr-amd.ini'
if (-not (Test-Path -LiteralPath $ini)) {
    Stop-WithMessage 'Der Paket-Installer meldete Erfolg, aber dlssnr-amd.ini fehlt. Bitte Rückfall ausführen.'
}
Copy-Item -LiteralPath $ini -Destination "$ini.tyria-backup" -Force

$presetScript = Join-Path $PSScriptRoot 'Set-NeuralPreset.ps1'
& $presetScript -Preset $Preset -GameExe $GameExe
if ($LASTEXITCODE -ne 0) {
    Stop-WithMessage "Preset $Preset konnte nicht angewendet werden. Sicherung: $ini.tyria-backup"
}

Write-Host ''
Write-Host "FERTIG: $Preset wurde reversibel in $gameDir installiert." -ForegroundColor Green
Write-Host 'Im Spiel: Home = ReShade/Add-ons, F6 = neuronalen Effekt umschalten, F5 = A/B-Screenshot.'
Write-Host "Rückfall: $runtime\install.ps1 -Target `"$GameExe`" -Route remove"

