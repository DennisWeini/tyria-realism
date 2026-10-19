param([Parameter(Mandatory=$true)][string]$PresentMonPath,
      [ValidateSet('Native1440','Baseline','Quality','Balanced')][string]$Label='Quality',
      [ValidateRange(5,180)][int]$Seconds=60)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!(Test-Path -LiteralPath $PresentMonPath -PathType Leaf)){throw 'PresentMon-Console-EXE fehlt. Offizielle Bezugsquelle: github.com/GameTechDev/PresentMon/releases'}
if(!(Get-Process 'Gw2-64' -ErrorAction SilentlyContinue)){throw 'GW2 läuft nicht. Keine Messung gestartet.'}
$signature=Get-AuthenticodeSignature -LiteralPath $PresentMonPath
if($signature.Status -ne 'Valid'){throw ('PresentMon-Signatur nicht gültig: '+$signature.Status+'. Keine Ausführung.')}
$folder=Join-Path $root ('reports\benchmarks\'+$Label+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force $folder | Out-Null
@{label=$Label;seconds=$Seconds;toolSha256=(Get-FileHash -LiteralPath $PresentMonPath -Algorithm SHA256).Hash;
 signer=$signature.SignerCertificate.Subject;method='ETW Present intervals, separate game and host CSVs; no injection';
 note='No automatic elevation; if access denied, stop and report. No latency or neural-quality proof.'} |
 ConvertTo-Json | Set-Content (Join-Path $folder 'capture.json') -Encoding UTF8
$session='TyriaRealism-'+[guid]::NewGuid().ToString('N')
# Console CLI documented at GameTechDev/PresentMon/README-ConsoleApplication.md.
# A fresh named session cannot stop an unrelated running PresentMon session.
& $PresentMonPath --process_name Gw2-64.exe --process_name Magpie.exe --multi_csv --v1_metrics --no_track_input --delay 5 --timed $Seconds --terminate_after_timed --session_name $session --output_file (Join-Path $folder 'frames.csv')
if($LASTEXITCODE -ne 0){throw "PresentMon meldet Fehler $LASTEXITCODE. Keine erfolgreiche Messung bestätigt. Siehe $folder"}
$files=@(Get-ChildItem $folder -Filter *.csv)
if(!$files){throw 'Keine CSV erzeugt. Keine FPS-Messung bestätigt.'}
foreach($file in $files){& (Join-Path $PSScriptRoot 'Analyze-PresentMon.ps1') -Csv $file.FullName}
Write-Host "Messdaten: $folder"

