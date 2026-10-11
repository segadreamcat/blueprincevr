@echo off
echo Checking local speech engine without opening the microphone...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0OfflineMemo.ps1" -CheckOnly
pause
