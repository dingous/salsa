@echo off
cd /d "%~dp0"
if not exist "CorporateUnity\Build\CorporateAvatarStudio.exe" (
  echo Compile o projeto CorporateUnity com Tools\build-corporate.ps1.
  pause
  exit /b 1
)
start "" "CorporateUnity\Build\CorporateAvatarStudio.exe"
