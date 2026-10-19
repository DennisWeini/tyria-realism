param(
 [string]$GameExe='',
 [string]$DependencyDirectory='',
 [switch]$ValidateOnly
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$root=Split-Path $PSScriptRoot -Parent
$feederUrl='https://github.com/jlrouzies-fr/DLSS5-Feeder/releases/download/v1.19.0/DLSS5-Feeder-1.19.0.zip'
$feederSha='9CC19E28F8FB4E216EA61621FDF6443E207E338EC639979AB335503C75F48F36'
$lumeniteCommit='25f40a2ac70bc90bd3dc53bb2fc9d5fa50536b8b'
$lumeniteUrl="https://github.com/umar-afzaal/LumeniteFX/archive/$lumeniteCommit.zip"
$lumeniteSha='06D40E49B54B7E2FCAE61B1F489B25A0C1779796160955760A3739B28CD103CC'

function Stop-Install([string]$Message){Write-Host "ABBRUCH: $Message" -ForegroundColor Red; exit 1}
function Assert-NvidiaSignature([string]$Path){
 $sig=Get-AuthenticodeSignature -LiteralPath $Path
 if($sig.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or !$sig.SignerCertificate -or $sig.SignerCertificate.Subject -notmatch 'NVIDIA Corporation'){
  Stop-Install "Keine gueltige NVIDIA-Authenticode-Signatur: $([IO.Path]::GetFileName($Path)) ($($sig.Status))."
 }
}
function Select-DependencyDirectory {
 Add-Type -AssemblyName System.Windows.Forms
 $dialog=New-Object System.Windows.Forms.FolderBrowserDialog
 $dialog.Description='Ordner mit eigenen NVIDIA-DLLs und genau einem DLSS5-Consumer auswählen'
 if($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK){Stop-Install 'Kein Abhaengigkeitsordner ausgewaehlt. Nichts wurde veraendert.'}
 return $dialog.SelectedPath
}
function Copy-Owned([string]$Source,[string]$Relative,[System.Collections.ArrayList]$Records){
 $destination=Join-Path $game $Relative
 $parent=Split-Path $destination -Parent
 New-Item -ItemType Directory -Force $parent | Out-Null
 $record=[ordered]@{path=$Relative;original=$false;backup='';installedSha256=''}
 if(Test-Path -LiteralPath $destination -PathType Leaf){
  $record.original=$true
  $record.backup=($Relative -replace '[\\/:*?"<>|]','_')
  Copy-Item -LiteralPath $destination -Destination (Join-Path $backup $record.backup)
 }
 Copy-Item -LiteralPath $Source -Destination $destination -Force
 $record.installedSha256=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
 [void]$Records.Add([pscustomobject]$record)
}

if(Get-Process Gw2-64 -ErrorAction SilentlyContinue){Stop-Install 'GW2 zuerst normal beenden.'}
try{$GameExe=Resolve-Gw2Exe $GameExe}catch{Stop-Install $_.Exception.Message}
$game=Split-Path $GameExe -Parent
$gpuNames=@(Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty Name)
if(-not ($gpuNames | Where-Object {$_ -match 'NVIDIA.*RTX\s*50\d{2}'})){
 Stop-Install ('Kein NVIDIA RTX-50-Adapter erkannt ('+($gpuNames -join ', ')+'). Der Installer schreibt auf dieser Hardware nichts.')
}
if(!$DependencyDirectory){$DependencyDirectory=Select-DependencyDirectory}
if(!(Test-Path -LiteralPath $DependencyDirectory -PathType Container)){Stop-Install 'Abhaengigkeitsordner fehlt.'}
foreach($name in @('nvngx_dlssnr.dll','nvngx_dlss.dll')){
 $candidate=Join-Path $DependencyDirectory $name
 if(!(Test-Path -LiteralPath $candidate -PathType Leaf)){Stop-Install "$name fehlt im ausgewaehlten Ordner."}
 Assert-NvidiaSignature $candidate
}
$nrVersion=(Get-Item -LiteralPath (Join-Path $DependencyDirectory 'nvngx_dlssnr.dll')).VersionInfo.FileVersionRaw
if(!$nrVersion -or $nrVersion -lt [version]'310.8.0.0'){Stop-Install "nvngx_dlssnr.dll ist zu alt oder ohne lesbare Version ($nrVersion)."}
$reno=@(Get-ChildItem -LiteralPath $DependencyDirectory -File -Filter 'renodx-dlss5*.addon64')
$chicken=Join-Path $DependencyDirectory 'deep-fried-chicken.addon64'
$hasChicken=Test-Path -LiteralPath $chicken -PathType Leaf
if($reno.Count -gt 1 -or ($reno.Count -eq 1 -and $hasChicken) -or ($reno.Count -eq 0 -and !$hasChicken)){
 Stop-Install 'Genau ein Consumer ist erforderlich: eine renodx-dlss5*.addon64 ODER Deep Fried Chicken.'
}
if($hasChicken){foreach($name in @('deep-fried-chicken-nvngx.dll','deep-fried-chicken.cfg')){if(!(Test-Path -LiteralPath (Join-Path $DependencyDirectory $name) -PathType Leaf)){Stop-Install "Deep-Fried-Chicken-Datei fehlt: $name"}}}
if(Get-ChildItem -LiteralPath $DependencyDirectory -File | Where-Object {$_.Name -match '^OptiScaler|dx11.*bridge'}){Stop-Install 'OptiScaler/DX11-Bridge im Quellordner erkannt. Diese Kombination wird nicht automatisch installiert.'}
$reshadeIni=Join-Path $game 'ReShade.ini'
if(!(Test-Path -LiteralPath $reshadeIni -PathType Leaf) -or !(Test-Path -LiteralPath (Join-Path $game 'reshade-shaders\Shaders\ReShade.fxh') -PathType Leaf)){
 Stop-Install 'Bestehendes ReShade mit Add-on-Unterstuetzung und Standard-Shaderordner wurde nicht gefunden.'
}
if(!(Test-Path -LiteralPath (Join-Path $game 'dxgi.dll') -PathType Leaf)){Stop-Install 'ReShade-DX11-Proxy dxgi.dll fehlt. Vorhandene Loader werden nicht automatisch ersetzt.'}
$conflicts=@(Get-ChildItem -LiteralPath $game -File | Where-Object {$_.Name -match '^(dlssg_to_fsr3|OptiScaler|dlssg-to-fsr3)|^(renodx-dlss5|deep-fried-chicken).*\.addon64$'})
if($conflicts.Count){Stop-Install ('Konflikt im Spielordner: '+(($conflicts.Name) -join ', ')+'. Erst vorhandenen Neural-Provider pruefen/entfernen.')}
Write-Host 'Abhaengigkeiten validiert:' -ForegroundColor Green
Write-Host "  GPU: $($gpuNames -join ', ')"
Write-Host "  NR: $nrVersion / $((Get-FileHash (Join-Path $DependencyDirectory 'nvngx_dlssnr.dll')).Hash)"
if($reno.Count){Write-Host "  Consumer: $($reno[0].Name) / $((Get-FileHash $reno[0].FullName).Hash)"}else{Write-Host "  Consumer: deep-fried-chicken.addon64 / $((Get-FileHash $chicken).Hash)"}
if($ValidateOnly){Write-Host 'Nur-Pruefung abgeschlossen; keine Dateien veraendert.';exit 0}

$backup=Join-Path $game '.tyria-realism-backups\nvidia-rtx50'
if(Test-Path -LiteralPath (Join-Path $backup 'receipt.json')){Stop-Install 'Eine RTX-50-Installation ist bereits protokolliert. Zuerst deinstallieren.'}
New-Item -ItemType Directory -Force $backup | Out-Null
$cache=Join-Path $env:LOCALAPPDATA ('TyriaRealism\NvidiaRTX50\'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $cache | Out-Null
$feederZip=Join-Path $cache 'DLSS5-Feeder-1.19.0.zip'
$lumeniteZip=Join-Path $cache "LumeniteFX-$lumeniteCommit.zip"
Invoke-WebRequest -Uri $feederUrl -OutFile $feederZip -UseBasicParsing
Invoke-WebRequest -Uri $lumeniteUrl -OutFile $lumeniteZip -UseBasicParsing
if((Get-FileHash $feederZip).Hash -ne $feederSha){Stop-Install 'DLSS5-Feeder-Pruefsumme stimmt nicht.'}
if((Get-FileHash $lumeniteZip).Hash -ne $lumeniteSha){Stop-Install 'LumeniteFX-Pruefsumme stimmt nicht.'}
$feeder=Join-Path $cache 'feeder';$lumenite=Join-Path $cache 'lumenite'
Expand-Archive $feederZip $feeder;Expand-Archive $lumeniteZip $lumenite
$lumeniteRoot=Get-ChildItem $lumenite -Directory | Select-Object -First 1
$records=[System.Collections.ArrayList]::new()
Copy-Owned (Join-Path $feeder 'dlss5-feed.addon64') 'dlss5-feed.addon64' $records
Copy-Owned (Join-Path $feeder 'reshade-shaders\Shaders\DLSS5_Feed.fx') 'reshade-shaders\Shaders\DLSS5_Feed.fx' $records
Copy-Owned (Join-Path $feeder 'Verify-DLSS5Feeder.ps1') '_TyriaRealismRuntime\NvidiaRTX50\Verify-DLSS5Feeder.ps1' $records
Copy-Owned (Join-Path $lumeniteRoot.FullName 'Shaders\lumenite_Kernel.fx') 'reshade-shaders\Shaders\lumenite_Kernel.fx' $records
Get-ChildItem (Join-Path $lumeniteRoot.FullName 'Shaders\include') -File | ForEach-Object {Copy-Owned $_.FullName ('reshade-shaders\Shaders\include\'+$_.Name) $records}
Copy-Owned (Join-Path $lumeniteRoot.FullName 'Textures\lumenite_bluenoise256.png') 'reshade-shaders\Textures\lumenite_bluenoise256.png' $records
Copy-Owned (Join-Path $lumeniteRoot.FullName 'LICENSE.md') '_TyriaRealismRuntime\NvidiaRTX50\LumeniteFX-LICENSE.md' $records
Copy-Owned (Join-Path $lumeniteRoot.FullName 'NOTICE') '_TyriaRealismRuntime\NvidiaRTX50\LumeniteFX-NOTICE.txt' $records
foreach($name in @('nvngx_dlssnr.dll','nvngx_dlss.dll')){Copy-Owned (Join-Path $DependencyDirectory $name) $name $records}
if($reno.Count){Copy-Owned $reno[0].FullName $reno[0].Name $records}else{foreach($name in @('deep-fried-chicken.addon64','deep-fried-chicken-nvngx.dll','deep-fried-chicken.cfg')){Copy-Owned (Join-Path $DependencyDirectory $name) $name $records}}
Copy-Owned (Join-Path $root 'presets\Tyria-NVIDIA-RTX50-Neural.ini') 'Tyria-NVIDIA-RTX50-Neural.ini' $records
Copy-Item $reshadeIni (Join-Path $backup 'ReShade.ini')
$iniText=Get-Content $reshadeIni -Raw
if($iniText -notmatch '(?m)^PresetPath='){Stop-Install 'PresetPath fehlt unerwartet in ReShade.ini; Dateien anhand Beleg deinstallieren.'}
Set-Content $reshadeIni ([regex]::Replace($iniText,'(?m)^PresetPath=.*$','PresetPath=.\Tyria-NVIDIA-RTX50-Neural.ini'))
$receipt=[ordered]@{schema=1;installed=(Get-Date -Format o);variant='nvidia-rtx50-feeder';sources=@{feederVersion='1.19.0';feederSha256=$feederSha;lumeniteCommit=$lumeniteCommit;lumeniteSha256=$lumeniteSha};files=$records}
$receipt | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $backup 'receipt.json')
Write-Host 'RTX-50-Feeder wurde vorbereitet. Das ist noch kein Laufzeitnachweis.' -ForegroundColor Green
Write-Host 'Nach dem Start dlss5-feed.log pruefen: Feature 18, Modell erstellt und fortlaufende Neural-Frames muessen sichtbar sein.'
Write-Host 'Home oeffnet ReShade. Lumenite Kernel muss vor DLSS5 Feed laufen; MSAA in GW2 ausschalten.'

