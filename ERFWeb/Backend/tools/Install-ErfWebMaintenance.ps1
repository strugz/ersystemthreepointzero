<#
.SYNOPSIS
    Sets up (or removes) the ERFWeb nightly database backup and the health monitor as Windows scheduled tasks.

.DESCRIPTION
    Run once on the API server from an elevated PowerShell (or double-click Install-ErfWebMaintenance.cmd in a
    release folder). It copies the maintenance scripts to -ToolsPath, so the tasks keep working when release
    folders are deleted, and registers:
      * "ERFWeb Database Backup": Backup-ErfWebDatabase.ps1 every night at -BackupTime;
      * "ERFWeb Health Check":    Test-ErfWebHealth.ps1 every 5 minutes.
    Both email -AlertTo through the Smtp section of the server's appsettings.json.

    The tasks run as -RunAsUser (asked for its password), or as SYSTEM when none is given. SYSTEM reaches a
    network share as the server's computer account, so with -CopyTo either give that account write access to
    the share or run the tasks as a service account that has it. The account needs read access to the API
    folder's appsettings.json and write access to -BackupDir.

    Running it again replaces both tasks with the new settings. -Uninstall removes them (backups are kept).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-ErfWebMaintenance.ps1 -CopyTo \\fileserver\backups\erfweb
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-ErfWebMaintenance.ps1 -CopyTo \\fileserver\backups\erfweb -RunAsUser MDMPI\svc-erfweb
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-ErfWebMaintenance.ps1 -Uninstall
#>
[CmdletBinding()]
param(
    [string] $InstallPath = 'C:\inetpub\wwwroot\erf_web-app',
    [string] $ToolsPath = 'C:\ProgramData\ERFWeb\tools',
    [string] $BackupDir = 'D:\ERFWeb-Backups',
    [string] $CopyTo,
    [int] $KeepDays = 14,
    [string] $BackupTime = '01:00',
    [string] $HealthUrl = 'https://sr.mdmpi.com.ph/erf/health',
    [string[]] $AlertTo = @('jca.mdmpi@marsmandrysdale.com'),
    [string] $PgBin,
    [string] $RunAsUser,
    [switch] $Uninstall
)

$ErrorActionPreference = 'Stop'
$backupTask = 'ERFWeb Database Backup'
$healthTask = 'ERFWeb Health Check'

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script from an elevated PowerShell (Run as administrator).'
}

foreach ($name in @($backupTask, $healthTask)) {
    if (Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue) {
        Unregister-ScheduledTask -TaskName $name -Confirm:$false
        Write-Host "Removed the old '$name' task."
    }
}
if ($Uninstall) {
    Write-Host "Done. Backups in $BackupDir and the scripts in $ToolsPath were left in place."
    return
}

# ---------- scripts in a fixed place ----------
New-Item -ItemType Directory -Force -Path $ToolsPath | Out-Null
foreach ($script in @('ErfWebMaintenance.Common.ps1', 'Backup-ErfWebDatabase.ps1', 'Test-ErfWebHealth.ps1')) {
    Copy-Item -Path (Join-Path $PSScriptRoot $script) -Destination $ToolsPath -Force
}
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

function Quote([string] $value) { return '"' + $value + '"' }

$alertList = ($AlertTo | ForEach-Object { "'$_'" }) -join ','
$backupArgs = "-NoProfile -ExecutionPolicy Bypass -File $(Quote (Join-Path $ToolsPath 'Backup-ErfWebDatabase.ps1'))" +
    " -InstallPath $(Quote $InstallPath) -BackupDir $(Quote $BackupDir) -KeepDays $KeepDays -AlertTo $alertList"
if ($CopyTo) { $backupArgs += " -CopyTo $(Quote $CopyTo)" }
if ($PgBin) { $backupArgs += " -PgBin $(Quote $PgBin)" }

$healthArgs = "-NoProfile -ExecutionPolicy Bypass -File $(Quote (Join-Path $ToolsPath 'Test-ErfWebHealth.ps1'))" +
    " -InstallPath $(Quote $InstallPath) -HealthUrl $(Quote $HealthUrl) -BackupDir $(Quote $BackupDir) -AlertTo $alertList"

$backupAction = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $backupArgs
$healthAction = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $healthArgs

$backupTrigger = New-ScheduledTaskTrigger -Daily -At $BackupTime
$healthTrigger = New-ScheduledTaskTrigger -Once -At (Get-Date).Date.AddMinutes(1) `
    -RepetitionInterval (New-TimeSpan -Minutes 5) -RepetitionDuration (New-TimeSpan -Days 3650)

$backupSettings = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 2) -MultipleInstances IgnoreNew
$healthSettings = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Minutes 4) -MultipleInstances IgnoreNew

if ($RunAsUser) {
    $credential = Get-Credential -UserName $RunAsUser -Message "Password of $RunAsUser, the account the ERFWeb maintenance tasks run as"
    $password = $credential.GetNetworkCredential().Password
    Register-ScheduledTask -TaskName $backupTask -Action $backupAction -Trigger $backupTrigger -Settings $backupSettings `
        -User $RunAsUser -Password $password -RunLevel Highest -Description 'Nightly pg_dump of the ERFWeb database (Backup-ErfWebDatabase.ps1).' | Out-Null
    Register-ScheduledTask -TaskName $healthTask -Action $healthAction -Trigger $healthTrigger -Settings $healthSettings `
        -User $RunAsUser -Password $password -RunLevel Highest -Description 'ERFWeb health and backup monitor (Test-ErfWebHealth.ps1).' | Out-Null
}
else {
    $system = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
    Register-ScheduledTask -TaskName $backupTask -Action $backupAction -Trigger $backupTrigger -Settings $backupSettings `
        -Principal $system -Description 'Nightly pg_dump of the ERFWeb database (Backup-ErfWebDatabase.ps1).' | Out-Null
    Register-ScheduledTask -TaskName $healthTask -Action $healthAction -Trigger $healthTrigger -Settings $healthSettings `
        -Principal $system -Description 'ERFWeb health and backup monitor (Test-ErfWebHealth.ps1).' | Out-Null
}

Write-Host ''
Write-Host "Registered '$backupTask' (every night at $BackupTime) and '$healthTask' (every 5 minutes)."
Write-Host "Backups: $BackupDir$(if ($CopyTo) { " and $CopyTo" }), kept $KeepDays days. Alerts to: $($AlertTo -join ', ')."
Write-Host ''
Write-Host "Run a backup now to check it works:  Start-ScheduledTask -TaskName '$backupTask'"
Write-Host "Then read $BackupDir\backup-log.txt."
