$ErrorActionPreference='Stop'
$gpus=@(Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty Name)
Write-Host 'Erkannte Grafikadapter:' -ForegroundColor Cyan
$gpus | ForEach-Object { Write-Host "  $_" }
Write-Host ''
foreach($gpu in $gpus){
 if($gpu -match 'Radeon.*RX\s*9\d{3}'){
  Write-Host "$gpu -> Universal-Profile; DLSSNR-AMD ist fuer RDNA4 vorgesehen. Projekt-Livetest nur RX 9070 XT."
 } elseif($gpu -match 'Radeon.*RX\s*[67]\d{3}'){
  Write-Host "$gpu -> Universal-Profile. Der GW2/DX11-Neuralpfad dieses Pakets ist fuer diese Generation nicht nachgewiesen."
 } elseif($gpu -match 'NVIDIA.*RTX\s*50\d{2}'){
  Write-Host "$gpu -> Universal-Profile; experimentelle RTX-50-Feeder-Vorbereitung verfuegbar, hier nicht auf NVIDIA-Hardware live getestet."
 } elseif($gpu -match 'NVIDIA.*RTX\s*[234]0\d{2}'){
  Write-Host "$gpu -> Universal-Profile. Neural nur als manueller Forschungsweg mit externer modifizierter DLL; nicht gebuendelt oder unterstuetzt."
 } elseif($gpu -match 'NVIDIA|Intel'){
  Write-Host "$gpu -> Universal-Profile als Kandidat; kein Neural-Livetest dieses Projekts."
 } else {
  Write-Host "$gpu -> Universal-Profile moeglich; nicht getestet."
 }
}

