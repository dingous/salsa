@echo off
cd /d "%~dp0"
if not exist "Tracker\.venv\Scripts\python.exe" (
    echo Execute Tracker\setup.ps1 para preparar o ambiente.
    pause
    exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "Web\start.ps1"
