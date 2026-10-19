param([string]$GameExe = '')

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

function Stop-WithMessage([string]$Message) {
    Write-Host "ABBRUCH: $Message" -ForegroundColor Red
    exit 1
}

if (Get-Process -Name 'Gw2-64' -ErrorAction SilentlyContinue) {
    Stop-WithMessage 'Guild Wars 2 läuft noch. Bitte normal beenden und erneut starten.'
}
try { $GameExe = Resolve-Gw2Exe $GameExe } catch { Stop-WithMessage $_.Exception.Message }

$project = Split-Path -Parent $PSScriptRoot
$installer = Join-Path $project 'runtime\DLSSNR-AMD\install.ps1'
if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) {
    Stop-WithMessage 'Der beim Einrichten geladene DLSSNR-AMD-Installer fehlt. Bitte den Paketordner der Installation verwenden.'
}

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer -Target $GameExe -Route remove
if ($LASTEXITCODE -ne 0) {
    Stop-WithMessage "Der offizielle Rückfall meldete Code $LASTEXITCODE. Bitte das Installationsmanifest im Spielordner aufbewahren."
}
Write-Host 'FERTIG: Die durch DLSSNR-AMD verwalteten Dateien wurden entfernt bzw. aus Sicherungen wiederhergestellt.' -ForegroundColor Green

