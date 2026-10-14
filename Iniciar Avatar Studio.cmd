@echo off
cd /d "%~dp0"
if not exist "Build\AvatarStudio.exe" (
    echo O executavel ainda nao foi gerado. Abra Assets/Scenes/AvatarStudio.unity no Unity.
    pause
    exit /b 1
)
start "" "%~dp0Build\AvatarStudio.exe"
