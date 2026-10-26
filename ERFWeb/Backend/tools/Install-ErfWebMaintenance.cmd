@echo off
rem Sets up the nightly ERFWeb database backup and the health monitor as scheduled tasks.
rem 1. Change the SETTINGS below for this server (see README.txt, Steps 3 to 5).
rem 2. Save, double-click this file and click Yes (it asks for administrator rights).
rem It runs Install-ErfWebMaintenance.ps1 from the same folder as this file, wherever that is.

rem ---------- SETTINGS ----------
rem ERFWeb API folder (holds appsettings.json)
set "INSTALL_PATH=C:\inetpub\wwwroot\erf_web-app"
rem Where the backups go (on a drive that exists on this server)
set "BACKUP_DIR=C:\ERFWeb-Backups"
rem Folder with pg_dump.exe and pg_restore.exe, version 18 or newer (README.txt Step 3)
set "PG_BIN=C:\Program Files\PostgreSQL\18\pgAdmin 4\runtime"
rem Who gets the alert emails
set "ALERT_TO=jca.mdmpi@marsmandrysdale.com"
rem Optional second copy on a network share: a full \\server\folder path, never a drive letter.
rem Leave empty for backups on this server only (README.txt Step 5).
set "COPY_TO="
rem Optional account the tasks run as (asks for its password); empty = SYSTEM
set "RUN_AS_USER="
rem ---------- END OF SETTINGS ----------

net session >nul 2>&1
if errorlevel 1 (
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

set ARGS=-InstallPath "%INSTALL_PATH%" -BackupDir "%BACKUP_DIR%" -AlertTo "%ALERT_TO%"
if defined PG_BIN set ARGS=%ARGS% -PgBin "%PG_BIN%"
if defined COPY_TO set ARGS=%ARGS% -CopyTo "%COPY_TO%"
if defined RUN_AS_USER set ARGS=%ARGS% -RunAsUser "%RUN_AS_USER%"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-ErfWebMaintenance.ps1" %ARGS%
pause
