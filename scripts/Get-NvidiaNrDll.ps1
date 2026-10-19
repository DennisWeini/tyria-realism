param(
    [string]$ArchivePath = '',
    [string]$CacheDirectory = (Join-Path $env:LOCALAPPDATA 'TyriaRealism\Dependencies\NvidiaNR-310.8.0')
)
# Optional third-party dependency retrieval. Not a license grant or a model redistributor.
$ErrorActionPreference = 'Stop'
$url = 'https://github.com/RankFTW/rhi-repo/releases/download/dlssnr-310.8.0/nvngx_dlssnr_310.8.0.zip'
$archiveHash = '388C0A7912E15EC911B9C9E11A692142B11FE387DDF2B637D8C358138FFFB3AC'
$dllHash = 'E16BCF15E16E13F527491CDF7845B2FE6521A738D8F7C9C721866A8496E1FC8E'
$CacheDirectory = [IO.Path]::GetFullPath($CacheDirectory)
New-Item -ItemType Directory -Force -Path $CacheDirectory | Out-Null
$dllPath = Join-Path $CacheDirectory 'nvngx_dlssnr.dll'
function Assert-NvidiaDll([string]$Path) {
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $dllHash) { throw 'NVIDIA-DLL: SHA256 stimmt nicht.' }
    $sig = Get-AuthenticodeSignature -LiteralPath $Path
    if ($sig.Status -ne 'Valid' -or !$sig.SignerCertificate -or $sig.SignerCertificate.Subject -notmatch 'NVIDIA Corporation') {
        throw "NVIDIA-DLL: Signaturpruefung fehlgeschlagen ($($sig.Status))."
    }
}
if (Test-Path -LiteralPath $dllPath) {
    Assert-NvidiaDll $dllPath
    Write-Output $dllPath
    exit
}
if (!$ArchivePath) {
    $ArchivePath = Join-Path $CacheDirectory 'nvngx_dlssnr_310.8.0.zip'
    if (!(Test-Path -LiteralPath $ArchivePath)) {
        Write-Host 'Lade optionale NVIDIA-NR-Abhaengigkeit von RankFTW/rhi-repo (GitHub), nicht von NVIDIA ...'
        $downloadPart = Join-Path $CacheDirectory ([Guid]::NewGuid().ToString() + '.download')
        try {
            Invoke-WebRequest -Uri $url -OutFile $downloadPart -UseBasicParsing
            if ((Get-FileHash -LiteralPath $downloadPart).Hash -ne $archiveHash) { throw 'Download-Pruefsumme stimmt nicht.' }
            Move-Item -LiteralPath $downloadPart -Destination $ArchivePath
        } finally {
            if (Test-Path -LiteralPath $downloadPart) { Remove-Item -LiteralPath $downloadPart }
        }
    }
}
if ((Get-FileHash -LiteralPath $ArchivePath).Hash -ne $archiveHash) { throw 'Archiv-Pruefsumme stimmt nicht; keine Extraktion.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $ArchivePath).Path)
$part = Join-Path $CacheDirectory ([Guid]::NewGuid().ToString() + '.dll')
try {
    $entries = @($zip.Entries | Where-Object { $_.Name -eq 'nvngx_dlssnr.dll' })
    if ($entries.Count -ne 1 -or $entries[0].Length -ne 165840496) { throw 'Unerwarteter Archivinhalt.' }
    # Extract only this exact entry to our own fixed directory; never use archive paths.
    [IO.Compression.ZipFileExtensions]::ExtractToFile($entries[0], $part, $false)
    Assert-NvidiaDll $part
    Move-Item -LiteralPath $part -Destination $dllPath
} finally {
    $zip.Dispose()
    if (Test-Path -LiteralPath $part) { Remove-Item -LiteralPath $part }
}
Write-Host 'Archiv, DLL-Hash und NVIDIA-Signatur geprueft. Keine Spieldatei veraendert.'
Write-Output $dllPath

