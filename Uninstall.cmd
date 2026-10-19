@echo off
setlocal
echo Close GW2 first. Choose the installed component to remove.
choice /C VAN /N /M "V = visual profiles, A = AMD neural, N = NVIDIA RTX 50: "
if errorlevel 3 goto nvidia
if errorlevel 2 goto amd
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Install-VisualComparison.ps1" -Restore
goto end
:amd
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Uninstall-DlssNrAmd.ps1"
goto end
:nvidia
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Uninstall-NvidiaRtx50.ps1"
:end
pause
