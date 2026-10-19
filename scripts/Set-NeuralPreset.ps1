param(
    [ValidateSet('Efficient', 'Quality', 'Balanced', 'Cinematic', 'RSR1080p')][string]$Preset = 'Balanced',
    [string]$GameExe = ''
)

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

$gameDir = Split-Path -Parent (Resolve-Path -LiteralPath $GameExe).Path
$ini = Join-Path $gameDir 'dlssnr-amd.ini'
$manifest = Join-Path $gameDir 'dlssnr-amd-install.txt'
if (-not (Test-Path -LiteralPath $ini -PathType Leaf) -or -not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    Stop-WithMessage 'Die geprüfte DLSSNR-AMD-Installation wurde im GW2-Ordner nicht gefunden.'
}

$profiles = @{
    Efficient = @{
        Int4Mixed = @{ Enabled = '1' }
        Network = @{ 'ACO Mode' = '1' }
        RenoDX = @{
            Enabled = '1'; NeuralUplift = '1'; NRIntensity = '0.70'; NRStyle = '1'; NRLocalTone = '0.85';
            NRLocalStructure = '0.85'; NRSkinStructure = '-0.50'; NRAutoMask = '1';
            NRResolutionScale = '0.60'; NRFollowInputRes = '0'; NRPasses = '1';
            NRStyleHighlightGuard = '0.30'; NRDetailStability = '2'; NRLookMode = '1';
            NRLookStrength = '1.00'; NRLookTone = '0.95'; NRLookDetail = '1.10';
            NRLookHalo = '0.15'; NRLookHaloBrightSide = '0.05'
        }
    }
    Quality = @{
        Int4Mixed = @{ Enabled = '0' }
        Network = @{ 'ACO Mode' = '0' }
        RenoDX = @{
            Enabled = '1'; NeuralUplift = '1'; NRIntensity = '0.85'; NRStyle = '0'; NRLocalTone = '0.85';
            NRLocalStructure = '0.70'; NRSkinStructure = '-0.50'; NRAutoMask = '0';
            NRResolutionScale = '1.00'; NRFollowInputRes = '0'; NRPasses = '1';
            NRStyleHighlightGuard = '0.20'; NRDetailStability = '1'; NRLookMode = '0';
            NRLookStrength = '1.00'; NRLookTone = '1.00'; NRLookDetail = '1.00';
            NRLookHalo = '0.00'; NRLookHaloBrightSide = '0.00'
        }
    }
    Balanced = @{
        Int4Mixed = @{ Enabled = '1' }
        Network = @{ 'ACO Mode' = '1' }
        RenoDX = @{
            Enabled = '1'; NeuralUplift = '1'; NRIntensity = '0.75'; NRStyle = '0'; NRLocalTone = '0.75';
            NRLocalStructure = '0.60'; NRSkinStructure = '-0.40'; NRAutoMask = '0';
            NRResolutionScale = '0.75'; NRFollowInputRes = '0'; NRPasses = '1';
            NRStyleHighlightGuard = '0.25'; NRDetailStability = '1'; NRLookMode = '0';
            NRLookStrength = '1.00'; NRLookTone = '1.00'; NRLookDetail = '1.00';
            NRLookHalo = '0.00'; NRLookHaloBrightSide = '0.00'
        }
    }
    Cinematic = @{
        Int4Mixed = @{ Enabled = '1' }
        Network = @{ 'ACO Mode' = '1' }
        RenoDX = @{
            Enabled = '1'; NeuralUplift = '1'; NRIntensity = '1.10'; NRStyle = '2';
            NRLocalTone = '1.05'; NRLocalStructure = '1.10'; NRSkinStructure = '-0.20';
            NRAutoMask = '1'; NRResolutionScale = '0.75'; NRFollowInputRes = '0'; NRPasses = '1';
            NRStyleHighlightGuard = '0.35'; NRDetailStability = '2'; NRLookMode = '1';
            NRLookStrength = '1.20'; NRLookTone = '1.05'; NRLookDetail = '1.35';
            NRLookHalo = '0.35'; NRLookHaloBrightSide = '0.20'
        }
    }
    RSR1080p = @{
        Int4Mixed = @{ Enabled = '1' }
        Network = @{ 'ACO Mode' = '1' }
        RenoDX = @{
            Enabled = '1'; NeuralUplift = '1'; NRIntensity = '1.10'; NRStyle = '2';
            NRLocalTone = '1.05'; NRLocalStructure = '1.10'; NRSkinStructure = '-0.20';
            NRAutoMask = '1'; NRResolutionScale = '1.00'; NRFollowInputRes = '1'; NRPasses = '1';
            NRStyleHighlightGuard = '0.35'; NRDetailStability = '2'; NRLookMode = '1';
            NRLookStrength = '1.20'; NRLookTone = '1.05'; NRLookDetail = '1.35';
            NRLookHalo = '0.35'; NRLookHaloBrightSide = '0.20'
        }
    }
}

$backupDir = Join-Path $gameDir '.tyria-realism-backups'
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
Copy-Item -LiteralPath $ini -Destination (Join-Path $backupDir "dlssnr-amd-$stamp.ini")

$wanted = $profiles[$Preset]
$lines = [System.IO.File]::ReadAllLines($ini)
$section = ''
$seen = @{}
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '^\s*\[([^]]+)\]') {
        $section = $Matches[1]
        continue
    }
    if ($wanted.ContainsKey($section) -and $lines[$i] -match '^\s*([^;#=]+?)\s*=') {
        $key = $Matches[1].Trim()
        if ($wanted[$section].ContainsKey($key)) {
            $lines[$i] = "$key = $($wanted[$section][$key])"
            $seen["$section/$key"] = $true
        }
    }
}

foreach ($sectionName in $wanted.Keys) {
    foreach ($key in $wanted[$sectionName].Keys) {
        if (-not $seen.ContainsKey("$sectionName/$key")) {
            Stop-WithMessage "Erwarteter Eintrag [$sectionName] $key fehlt. Die aktive Datei wurde nicht geschrieben."
        }
    }
}

[System.IO.File]::WriteAllLines($ini, $lines, (New-Object System.Text.UTF8Encoding($false)))
$iniHash = (Get-FileHash -LiteralPath $ini -Algorithm SHA256).Hash

Write-Host "FERTIG: Neural-Preset $Preset ist für den nächsten GW2-Start aktiv." -ForegroundColor Green
if ($Preset -eq 'Efficient') {
    Write-Host 'Aktiv: 60 Prozent Modellauflösung, ACO und INT4 mixed; natürlicher Detail-Look.'
    Write-Host 'Dieses neue Profil ist noch nicht im echten GW2-Lauf vermessen.'
} elseif ($Preset -eq 'Balanced') {
    Write-Host 'Aktiv: 75 Prozent Modellauflösung, ACO und INT4 mixed.'
    Write-Host 'Isolierter 1440p-Test: 7,92 ms statt 16,28 ms im vorherigen GW2-Quality-Lauf.'
} elseif ($Preset -eq 'Cinematic') {
    Write-Host 'Aktiv: schneller ACO/INT4-Unterbau mit deutlich kräftigerem Cinematic-Look.'
    Write-Host 'Das Preset verstärkt Struktur und feine Details, begrenzt aber typische dunkle Halos.'
} elseif ($Preset -eq 'RSR1080p') {
    Write-Host 'Aktiv: Neural Cinematic für 1920x1080 plus anschließendes Radeon Super Resolution auf 2560x1440.'
    Write-Host 'Jetzt in AMD Software für GW2 RSR und AFMF aktivieren und GW2 exklusiv fullscreen auf 1920x1080 stellen.'
} else {
    Write-Host 'Aktiv: volle Modellauflösung und Standardnetz.'
}
Write-Host "INI-SHA256: $iniHash"
Write-Host "Sicherung: $backupDir"


