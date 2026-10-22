@echo off
rem Double-click on the server to install the ERFWeb API release in this folder (asks for administrator rights).
net session >nul 2>&1
if errorlevel 1 (
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Deploy-ErfWeb.ps1"
pause
