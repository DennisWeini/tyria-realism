function Resolve-Gw2Exe([string]$GameExe) {
    if ($GameExe -and (Test-Path -LiteralPath $GameExe -PathType Leaf)) {
        $resolved = (Resolve-Path -LiteralPath $GameExe).Path
        if ([System.IO.Path]::GetFileName($resolved) -ine 'Gw2-64.exe') {
            throw 'Die angegebene Datei heißt nicht Gw2-64.exe. Es wurde nichts verändert.'
        }
        return $resolved
    }

    # Vortex installs the toolkit as <GW2>\_TyriaRealism\scripts.
    $vortexCandidate = Join-Path $PSScriptRoot '..\..\Gw2-64.exe'
    if (Test-Path -LiteralPath $vortexCandidate -PathType Leaf) {
        return (Resolve-Path -LiteralPath $vortexCandidate).Path
    }

    Add-Type -AssemblyName System.Windows.Forms
    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Title = 'Guild Wars 2: Gw2-64.exe auswählen'
    $dialog.Filter = 'Guild Wars 2 (Gw2-64.exe)|Gw2-64.exe'
    $dialog.CheckFileExists = $true
    if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
        throw 'Keine Gw2-64.exe ausgewählt. Es wurde nichts verändert.'
    }
    if ([System.IO.Path]::GetFileName($dialog.FileName) -ine 'Gw2-64.exe') {
        throw 'Die ausgewählte Datei heißt nicht Gw2-64.exe. Es wurde nichts verändert.'
    }
    return (Resolve-Path -LiteralPath $dialog.FileName).Path
}
