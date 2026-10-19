param([string]$GameExe='')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(Get-Process Gw2-64 -ErrorAction SilentlyContinue){throw 'GW2 zuerst normal beenden.'}
$GameExe=Resolve-Gw2Exe $GameExe
$game=Split-Path $GameExe -Parent
$backup=Join-Path $game '.tyria-realism-backups\nvidia-rtx50'
$receiptPath=Join-Path $backup 'receipt.json'
if(!(Test-Path $receiptPath -PathType Leaf)){throw 'Kein RTX-50-Installationsbeleg gefunden.'}
$receipt=Get-Content $receiptPath -Raw | ConvertFrom-Json
$allowed=@(
 'dlss5-feed.addon64','nvngx_dlssnr.dll','nvngx_dlss.dll','Tyria-NVIDIA-RTX50-Neural.ini',
 'deep-fried-chicken.addon64','deep-fried-chicken-nvngx.dll','deep-fried-chicken.cfg',
 'reshade-shaders\Shaders\DLSS5_Feed.fx','reshade-shaders\Shaders\lumenite_Kernel.fx',
 'reshade-shaders\Textures\lumenite_bluenoise256.png','_TyriaRealismRuntime\NvidiaRTX50\Verify-DLSS5Feeder.ps1',
 '_TyriaRealismRuntime\NvidiaRTX50\LumeniteFX-LICENSE.md','_TyriaRealismRuntime\NvidiaRTX50\LumeniteFX-NOTICE.txt'
)
$allowedRegex='^renodx-dlss5[^\\/]*\.addon64$|^reshade-shaders\\Shaders\\include\\lumenite_[^\\/]+\.fxh$'
foreach($f in $receipt.files){
 $rel=[string]$f.path
 if($rel -notin $allowed -and $rel -notmatch $allowedRegex){throw "Unbekannter Pfad im Beleg; Abbruch: $rel"}
 if([IO.Path]::IsPathRooted($rel) -or $rel -match '\.\.'){throw "Unsicherer Pfad im Beleg; Abbruch: $rel"}
 $path=Join-Path $game $rel
 if(Test-Path -LiteralPath $path -PathType Leaf){
  if((Get-FileHash $path).Hash -eq $f.installedSha256){Remove-Item -LiteralPath $path}else{Write-Warning "Veraenderte Datei bleibt erhalten: $rel";continue}
 }
 if($f.original -and $f.backup){Copy-Item -LiteralPath (Join-Path $backup $f.backup) -Destination $path -Force}
}
$reshadeIni=Join-Path $game 'ReShade.ini'
if(Test-Path (Join-Path $backup 'ReShade.ini')){Copy-Item (Join-Path $backup 'ReShade.ini') $reshadeIni -Force}
Rename-Item $receiptPath ('receipt.removed-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.json')
Write-Host 'RTX-50-Feeder-Dateien wurden anhand des Belegs entfernt bzw. wiederhergestellt.' -ForegroundColor Green
