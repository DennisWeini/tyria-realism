param([Parameter(Mandatory=$true)][string]$Csv)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$rows=@(Import-Csv -LiteralPath $Csv)
if(!$rows -or !($rows[0].PSObject.Properties.Name -contains 'MsBetweenPresents')){throw 'PresentMon-v1-Metrik MsBetweenPresents fehlt.'}
foreach($column in @('Application','ProcessID','SwapChainAddress')){if(!($rows[0].PSObject.Properties.Name -contains $column)){throw "Spalte fehlt: $column"}}
$groups=$rows | Group-Object Application,ProcessID,SwapChainAddress
$summary=@();$num=0
foreach($group in $groups){
    $valid=@();$rejected=0
    foreach($row in $group.Group){
        $v=0.0
        if([double]::TryParse($row.MsBetweenPresents,[Globalization.NumberStyles]::Float,[Globalization.CultureInfo]::InvariantCulture,[ref]$v) -and $v -gt 0 -and ![double]::IsInfinity($v) -and ![double]::IsNaN($v)){$valid+=,$v}else{$rejected++}
    }
    if($valid.Count -lt 2){$summary+=@{group=$group.Name;status='insufficient_samples';rejected=$rejected};continue}
    $num++;$id=[guid]::NewGuid().ToString('N').Substring(0,8)
    $dir=Join-Path $root 'reports\benchmarks\analysis'
    New-Item -ItemType Directory -Force $dir | Out-Null
    $intervals=Join-Path $dir "intervals-$id.csv"
    @('frameTimeMs')+@($valid | ForEach-Object {$_.ToString('R',[Globalization.CultureInfo]::InvariantCulture)}) | Set-Content $intervals -Encoding ascii
    $report=Join-Path $dir "statistics-$id.json"
    & (Join-Path $root 'bin\TyriaLab.exe') fps $intervals frameTimeMs $report
    if($LASTEXITCODE){throw 'FPS-Auswertung fehlgeschlagen.'}
    $summary+=@{group=$group.Name;status='analyzed';rejectedNonpositiveOrInvalid=$rejected;validSamples=$valid.Count;statistics=$report;
        warning='Swapchains nicht zusammenführen. Magpie-FPS sind keine GW2-FPS. Lade-/Warmup-Phase vor Aufnahme ausschließen.'}
}
$summary | ConvertTo-Json -Depth 6 | Set-Content ($Csv+'.analysis.json') -Encoding UTF8
if(!$num){throw 'Keine Gruppe mit genügend gültigen Messwerten.'}

