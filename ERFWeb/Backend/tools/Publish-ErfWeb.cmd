@echo off
rem Double-click to build the ERFWeb API release folder (ERFWeb\releases\ERFWeb-Api-<version>-<commit>).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Publish-ErfWeb.ps1"
pause
