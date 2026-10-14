@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\publish-windows.ps1"
if errorlevel 1 (
    echo Falha na publicacao. Consulte corporate-build.log.
    pause
    exit /b 1
)
echo Pronto: publish\Windows\AvatarStudio.exe
pause
