@echo off
rem First-time install only: double-click in a release folder on a new server. It extracts ERFWeb-Api.zip into
rem C:\inetpub\wwwroot\erf_web-app and registers the ERFWeb.Api Windows Service (asks for administrator rights).
rem For every later release use Deploy-ErfWeb.cmd instead.
net session >nul 2>&1
if errorlevel 1 (
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-ErfWebService.ps1" -ReleaseZip "%~dp0ERFWeb-Api.zip"
pause
