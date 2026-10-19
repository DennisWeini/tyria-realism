param([string]$OutputDirectory='')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$OutputDirectory){$OutputDirectory=Join-Path $root 'dist'}
$version=(Get-Content (Join-Path $root 'data\project.json') -Raw | ConvertFrom-Json).version
& node (Join-Path $root 'tools\generate-docs.mjs');if($LASTEXITCODE){throw 'Documentation generation failed.'}
& node (Join-Path $root 'tools\audit.mjs');if($LASTEXITCODE){throw 'Publication audit failed.'}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$stage=Join-Path $OutputDirectory ('stage-'+[guid]::NewGuid().ToString('N'))
$toolkit=Join-Path $stage '_TyriaRealism'
New-Item -ItemType Directory -Force $toolkit | Out-Null
foreach($dir in @('scripts','effects','presets','data','licenses')){if(Test-Path (Join-Path $root $dir)){Copy-Item (Join-Path $root $dir) $toolkit -Recurse}}
New-Item -ItemType Directory -Force (Join-Path $toolkit 'docs') | Out-Null
foreach($file in @('INSTALLATION.md','COMPATIBILITY.md','MODEL-SOURCES.md','NVIDIA-VARIANTS.md','NEXUS-DESCRIPTION.txt')){Copy-Item (Join-Path $root ('docs\'+$file)) (Join-Path $toolkit 'docs')}
foreach($file in @('Setup.cmd','Uninstall.cmd','README.md','LICENSE','THIRD-PARTY-NOTICES.md')){Copy-Item (Join-Path $root $file) $toolkit}
$files=Get-ChildItem $toolkit -Recurse -File
if($files | Where-Object Extension -in '.dll','.exe','.bin','.addon64','.addon32','.log','.csv','.dmp','.zip'){throw 'Unexpected binary/private payload.'}
$zip=Join-Path $OutputDirectory "TyriaRealism-$version-toolkit.zip"
if(Test-Path $zip){throw 'Versioned ZIP already exists; choose another output directory or version.'}
Compress-Archive -Path $toolkit -DestinationPath $zip
$hash=(Get-FileHash $zip).Hash
"$hash  $([IO.Path]::GetFileName($zip))" | Set-Content (Join-Path $OutputDirectory 'SHA256SUMS.txt')
Write-Host "$zip ($hash)"
