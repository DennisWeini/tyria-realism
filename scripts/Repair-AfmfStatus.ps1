$ErrorActionPreference = 'Stop'
function Stop-WithMessage([string]$Message) { Write-Host "ABBRUCH: $Message" -ForegroundColor Red; exit 1 }

if (Get-Process -Name 'Gw2-64' -ErrorAction SilentlyContinue) {
    Stop-WithMessage 'Guild Wars 2 läuft. Bitte zuerst normal beenden, damit kein Spielfortschritt verloren geht.'
}
$amdUi = 'C:\Program Files\AMD\CNext\CNext\RadeonSoftware.exe'
if (-not (Test-Path -LiteralPath $amdUi -PathType Leaf)) {
    Stop-WithMessage 'AMD Software wurde am erwarteten Ort nicht gefunden.'
}
$running = Get-Process -Name 'RadeonSoftware' -ErrorAction SilentlyContinue
if ($running) {
    $running | Stop-Process -Force
    Start-Sleep -Seconds 2
}
Start-Process -FilePath $amdUi
Start-Sleep -Seconds 5
if (-not (Get-Process -Name 'RadeonSoftware' -ErrorAction SilentlyContinue)) {
    Stop-WithMessage 'AMD Software konnte nicht neu gestartet werden.'
}
Write-Host 'FERTIG: AMD Software wurde neu gestartet. Das vorhandene GW2-Profil wurde nicht verändert.' -ForegroundColor Green
Write-Host 'Jetzt GW2 starten und den AFMF-Status mit Alt+R kontrollieren.'


