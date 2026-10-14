@echo off
cd /d "%~dp0"
if not exist "CorporateUnity\Build\CorporateAvatarStudio.exe" (
    echo Compile a versao MakeHuman com Tools\build-corporate.ps1.
    pause
    exit /b 1
)
start "" "%~dp0CorporateUnity\Build\CorporateAvatarStudio.exe"
