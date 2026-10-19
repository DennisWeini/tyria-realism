param([switch]$IncludePreview)
$ErrorActionPreference='Stop'
$releases=@(Invoke-RestMethod 'https://api.github.com/repos/DennisWeini/tyria-realism/releases?per_page=20')
$release=$releases | Where-Object { !$_.draft -and ($IncludePreview -or !$_.prerelease) } | Sort-Object published_at -Descending | Select-Object -First 1
if(!$release){Write-Host 'Noch kein passendes Release. Aktuelle Dokumentation: https://github.com/DennisWeini/tyria-realism'; exit}
Write-Host ('Verfuegbar: '+$release.tag_name)
Write-Host $release.html_url
Write-Host 'Es wurde nichts heruntergeladen oder installiert. Erst Aenderungen und Kompatibilitaet lesen.'

