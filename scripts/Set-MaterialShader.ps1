param(
    [ValidateSet('Natural','Detail','Off')][string]$Preset = 'Natural',
    [string]$GameExe = ''
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

function Stop-WithMessage([string]$Message) {
    Write-Host "ABBRUCH: $Message" -ForegroundColor Red
    exit 1
}

if (Get-Process -Name 'Gw2-64' -ErrorAction SilentlyContinue) {
    Stop-WithMessage 'Guild Wars 2 läuft. Bitte normal beenden; es wurde nichts verändert.'
}
try { $GameExe = Resolve-Gw2Exe $GameExe } catch { Stop-WithMessage $_.Exception.Message }
$gameDir = Split-Path -Parent (Resolve-Path -LiteralPath $GameExe).Path
$project = Split-Path -Parent $PSScriptRoot
$source = Join-Path $project 'effects\TyriaMaterials.fx'
$shaderDir = Join-Path $gameDir 'reshade-shaders\Shaders'
$target = Join-Path $shaderDir 'TyriaMaterials.fx'
$presetFile = Join-Path $gameDir 'ReShadePreset.ini'
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { Stop-WithMessage "Shaderquelle fehlt: $source" }
if (-not (Test-Path -LiteralPath $presetFile -PathType Leaf)) { Stop-WithMessage 'ReShadePreset.ini wurde nicht gefunden.' }

$backupDir = Join-Path $gameDir '.tyria-realism-backups'
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$presetBackup = Join-Path $backupDir "materials-$stamp-ReShadePreset.ini"
Copy-Item -LiteralPath $presetFile -Destination $presetBackup
New-Item -ItemType Directory -Path $shaderDir -Force | Out-Null

$lines = [Collections.Generic.List[string]]::new()
$lines.AddRange([string[]][IO.File]::ReadAllLines($presetFile))
$technique = 'TyriaMaterials@TyriaMaterials.fx'
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^Techniques=(.*)$') {
        $items = @($Matches[1].Split(',',[StringSplitOptions]::RemoveEmptyEntries))
        if ($Preset -eq 'Off') { $items = @($items | Where-Object { $_ -ne $technique }) }
        elseif ($items -notcontains $technique) { $items += $technique }
        $lines[$i] = 'Techniques=' + ($items -join ',')
    }
    if ($lines[$i] -match '^TechniqueSorting=(.*)$' -and $Preset -ne 'Off') {
        $items = @($Matches[1].Split(',',[StringSplitOptions]::RemoveEmptyEntries))
        if ($items -notcontains $technique) { $items += $technique }
        $lines[$i] = 'TechniqueSorting=' + ($items -join ',')
    }
}

$settings = if ($Preset -eq 'Detail') {
    @{ NaturalGrade='0.000000'; FineDetail='0.920000'; BroadDetail='0.380000'; DetailLimit='0.046000'; DarkProtection='0.060000'; HighlightProtection='0.860000'; Colourfulness='0.010000'; UiProtection='0.750000' }
} else {
    @{ NaturalGrade='0.700000'; FineDetail='0.460000'; BroadDetail='0.180000'; DetailLimit='0.034000'; DarkProtection='0.055000'; HighlightProtection='0.880000'; Colourfulness='0.010000'; UiProtection='0.750000' }
}

if ($Preset -ne 'Off') {
    $sectionStart = -1
    for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i] -eq '[TyriaMaterials.fx]') { $sectionStart = $i; break } }
    if ($sectionStart -lt 0) {
        $lines.Add('')
        $lines.Add('[TyriaMaterials.fx]')
        foreach ($key in $settings.Keys) { $lines.Add("$key=$($settings[$key])") }
    } else {
        $seen = @{}
        for ($i = $sectionStart + 1; $i -lt $lines.Count -and $lines[$i] -notmatch '^\['; $i++) {
            if ($lines[$i] -match '^([^=]+)=') { $key=$Matches[1]; if($settings.ContainsKey($key)){ $lines[$i]="$key=$($settings[$key])"; $seen[$key]=$true } }
        }
        foreach ($key in $settings.Keys) { if(-not $seen[$key]){ $lines.Insert($sectionStart + 1,"$key=$($settings[$key])") } }
    }
}

$temp = "$presetFile.tyria-new"
[IO.File]::WriteAllLines($temp,$lines,(New-Object Text.UTF8Encoding($false)))
Move-Item -LiteralPath $temp -Destination $presetFile -Force
if ($Preset -ne 'Off') {
    Copy-Item -LiteralPath $source -Destination "$target.new" -Force
    Move-Item -LiteralPath "$target.new" -Destination $target -Force
}

$receipt = [ordered]@{
    preset=$Preset; installedAt=[DateTimeOffset]::Now; shader=$target
    shaderSha256=if(Test-Path -LiteralPath $target){(Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash}else{$null}
    presetBackup=$presetBackup
    note='Off removes the technique from the active preset but keeps the owned shader for easy reactivation.'
}
$receipt | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backupDir 'materials-last-change.json') -Encoding utf8
Write-Host "FERTIG: Tyria Materials $Preset ist für den nächsten GW2-Start gesetzt." -ForegroundColor Green
Write-Host "Sicherung: $presetBackup"

