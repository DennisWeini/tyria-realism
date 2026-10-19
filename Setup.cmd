@echo off
setlocal
echo Tyria Realism - experimental preview
echo 1 - Visual comparison (existing ReShade required, no NVIDIA model)
echo 2 - Neural setup with your own compatible DLL
echo 3 - Neural setup with optional RHI third-party download
echo 4 - Check available releases (no automatic installation)
choice /C 1234 /N /M "Choose 1-4: "
if errorlevel 4 goto updates
if errorlevel 3 goto rhi
if errorlevel 2 goto own
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Install-VisualComparison.ps1"
goto end
:own
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Install-DlssNrAmd.ps1"
goto end
:rhi
echo Third-party source: RankFTW/rhi-repo, not an official NVIDIA download.
echo Read docs\MODEL-SOURCES.md. NVIDIA authorization is not established here.
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Install-DlssNrAmd.ps1" -DownloadModelFromRhi
goto end
:updates
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Check-Updates.ps1" -IncludePreview
:end
echo.
pause

