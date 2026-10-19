@echo off
setlocal
echo Close GW2 first. Restore visual profiles before uninstalling neural runtime.
choice /C VN /N /M "V = restore visual comparison, N = remove neural runtime: "
if errorlevel 2 goto neural
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Install-VisualComparison.ps1" -Restore
goto end
:neural
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Uninstall-DlssNrAmd.ps1"
:end
pause

