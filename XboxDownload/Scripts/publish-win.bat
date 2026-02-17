@echo off
set SCRIPT_DIR=%~dp0
PowerShell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%publish-win.ps1"
pause