param([string]$GameExe='', [switch]$Restore)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
if(Get-Process Gw2-64 -ErrorAction SilentlyContinue){throw 'GW2 zuerst beenden.'}
$GameExe=Resolve-Gw2Exe $GameExe
$game=Split-Path $GameExe -Parent
$root=Split-Path $PSScriptRoot -Parent
$backup=Join-Path $game '.tyria-realism-backups\public-elora-comparison'
$ini=Join-Path $game 'ReShade.ini'
$target=Join-Path $game 'reshade-shaders\Shaders\TyriaEloraPublic'
if($Restore){
 if(!(Test-Path (Join-Path $backup 'receipt.json'))){throw 'Kein Importbeleg gefunden.'}
 $receipt=Get-Content (Join-Path $backup 'receipt.json') -Raw | ConvertFrom-Json
 if(Test-Path $ini){Copy-Item $ini (Join-Path $backup 'ReShade-after-test.ini') -Force}
 Copy-Item (Join-Path $backup 'ReShade.ini') $ini -Force
 foreach($f in $receipt.files){
   $path=Join-Path $game $f.path
   if((Test-Path -LiteralPath $path) -and (Get-FileHash -LiteralPath $path).Hash -eq $f.sha256){Remove-Item -LiteralPath $path}
 }
 Write-Host 'Vorheriges Preset wiederhergestellt. Veraenderte Dateien und Backups bleiben erhalten.'
 exit
}
if(!(Test-Path $ini)){throw 'ReShade zuerst aus offizieller Quelle einrichten; ReShade.ini fehlt.'}
if((Test-Path $backup) -or (Test-Path $target)){throw 'Vorhandenen Import zuerst pruefen oder wiederherstellen.'}
$text=Get-Content $ini -Raw
if($text -notmatch '(?m)^PresetPath='){throw 'PresetPath fehlt.'}
$shaderMap=@{'BloomingHDR.fx'='AstrayFX';'NeoSSAO.fx'='Barbatos';'Reinhard.fx'='GShade-Shaders';'LocalContrast.fx'='GShade-Shaders'}
$manifest=Get-Content (Join-Path $root 'data\elora-shaders.json') -Raw | ConvertFrom-Json
$cache=Join-Path $env:LOCALAPPDATA ('TyriaRealism\Shaders\'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $cache | Out-Null
foreach($entry in $manifest){
 $url='https://raw.githubusercontent.com/AlteredM1nd/gw2-reshade-eloras-personal-presets/95eb5dcf53808c8eb7651a76e473b9bbf77afb05/reshade-shaders/Shaders/'+$shaderMap[$entry.name]+'/'+$entry.name
 $path=Join-Path $cache $entry.name
 Invoke-WebRequest -Uri $url -OutFile $path -UseBasicParsing
 if((Get-FileHash $path).Hash -ne $entry.sha256){throw "Shader-Pruefsumme stimmt nicht: $($entry.name)"}
}
$names=@('Tyria-Elora-Balanced.ini','Tyria-Elora-Quality-DepthTest.ini')
foreach($name in $names){if(Test-Path (Join-Path $game $name)){throw 'Ein Zielpreset existiert bereits; nichts ueberschreiben.'}}
New-Item -ItemType Directory -Force $backup | Out-Null
Copy-Item $ini (Join-Path $backup 'ReShade.ini')
if((Get-FileHash $ini).Hash -ne (Get-FileHash (Join-Path $backup 'ReShade.ini')).Hash){throw 'Backup-Pruefung fehlgeschlagen.'}
New-Item -ItemType Directory -Force $target | Out-Null
$files=@()
foreach($entry in $manifest){$path=Join-Path $target $entry.name; Copy-Item (Join-Path $cache $entry.name) $path; $files+=@{path=('reshade-shaders\Shaders\TyriaEloraPublic\'+$entry.name);sha256=(Get-FileHash $path).Hash}}
foreach($name in $names){$path=Join-Path $game $name; Copy-Item (Join-Path $root ('presets\'+$name)) $path; $files+=@{path=$name;sha256=(Get-FileHash $path).Hash}}
@{files=$files;source='Elora v5.0.0 commit95eb5dc';date=(Get-Date -Format o)} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $backup 'receipt.json')
Set-Content $ini ([regex]::Replace($text,'(?m)^PresetPath=.*$','PresetPath=.\Tyria-Elora-Balanced.ini'))
Write-Host 'Balanced-Vergleich eingerichtet. Neural fuer den ersten Vergleich ausschalten. HUD/Depth/FPS im Spiel pruefen.'

