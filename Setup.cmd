@echo off
setlocal
echo Tyria Realism - experimental preview
echo 1 - Universal Lite visual profile (all GPUs, existing ReShade)
echo 2 - AMD RX 9000 neural setup with your own compatible DLL
echo 3 - AMD RX 9000 neural setup with optional RHI third-party download
echo 4 - NVIDIA RTX 50 experimental neural preparation
echo 5 - Show detected hardware and recommended path
echo 6 - Check available releases (no automatic installation)
echo 7 - Apply Natural Personal preset (existing AMD neural installation)
choice /C 1234567 /N /M "Choose 1-7: "
if errorlevel 7 goto natural
if errorlevel 6 goto updates
if errorlevel 5 goto hardware
if errorlevel 4 goto nvidia
if errorlevel 3 goto rhi
if errorlevel 2 goto own
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Install-VisualComparison.ps1" -Profile Lite
goto end
:own
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Install-DlssNrAmd.ps1"
goto end
:rhi
echo Third-party source: RankFTW/rhi-repo, not an official NVIDIA download.
echo Read docs\MODEL-SOURCES.md. NVIDIA authorization is not established here.
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Install-DlssNrAmd.ps1" -DownloadModelFromRhi
goto end
:nvidia
echo Requires your own signed NVIDIA DLLs and exactly one separately obtained neural consumer.
echo RTX 20-40 modified DLLs are not downloaded or bundled; read docs\NVIDIA-VARIANTS.md.
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Install-NvidiaRtx50.ps1"
goto end
:hardware
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Show-HardwareSupport.ps1"
goto end
:natural
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0scripts\Set-NeuralPreset.ps1" -Preset NaturalPersonal
goto end
:updates
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Check-Updates.ps1" -IncludePreview
:end
echo.
pause
