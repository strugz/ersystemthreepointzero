<#
.SYNOPSIS
    Health monitor for ERFWeb (plan Phase 8.6): emails when the API goes down or the nightly backup is missing,
    and again when things are back to normal.

.DESCRIPTION
    Run on the API server every 5 minutes by the "ERFWeb Health Check" scheduled task
    (Install-ErfWebMaintenance.ps1). Two checks:
      * the public health address (default https://sr.mdmpi.com.ph/erf/health) must answer 200 with
        "status":"ok". That covers nginx, the API service and the database together. A problem is reported
        after -FailuresBeforeAlert checks in a row (default 2, so about 10 minutes), so a restart during a
        deploy does not raise an alarm;
      * the newest backup in -BackupDir must be younger than -BackupMaxAgeHours (default 26), which catches a
        backup task that silently stopped running.
    Each problem emails once when it starts and once when it clears; the state is kept in -StateFile. Every
    check is logged to <StateFile folder>\health-log.txt only when something changes.
    Runs on the API server itself, so it cannot report the whole server being down; IT's own monitoring
    should cover that.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\Test-ErfWebHealth.ps1 -AlertTo you@example.com
#>
[CmdletBinding()]
param(
    [string] $InstallPath = 'C:\inetpub\wwwroot\erf_web-app',
    [string] $HealthUrl = 'https://sr.mdmpi.com.ph/erf/health',
    [string] $BackupDir = 'D:\ERFWeb-Backups',
    [int] $BackupMaxAgeHours = 26,
    [int] $FailuresBeforeAlert = 2,
    [string] $StateFile = 'C:\ProgramData\ERFWeb\health-state.json',
    [string[]] $AlertTo = @()
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ErfWebMaintenance.Common.ps1')

$logFile = Join-Path (Split-Path -Parent $StateFile) 'health-log.txt'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# ---------- the checks ----------

function Test-Api {
    try {
        $response = Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing -TimeoutSec 20
        $health = $response.Content | ConvertFrom-Json
        if ($health.status -eq 'ok') { return $null }
        return "the API answered but is not healthy (database: $($health.database))"
    }
    catch {
        $status = $null
        if ($_.Exception.Response) { $status = [int] $_.Exception.Response.StatusCode }
        if ($status -eq 503) {
            try {
                $reader = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())
                $health = $reader.ReadToEnd() | ConvertFrom-Json
                return "the API answered 503 (database: $($health.database))"
            }
            catch { return 'the API answered 503' }
        }
        if ($status) { return "the API answered HTTP $status" }
        return "no answer from $HealthUrl ($($_.Exception.GetType().Name))"
    }
}

function Test-Backup {
    if (-not (Test-Path $BackupDir)) { return "the backup folder $BackupDir does not exist" }
    $newest = Get-ChildItem -Path $BackupDir -Filter '*.dump' -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $newest) { return "there is no backup in $BackupDir" }
    $age = (Get-Date) - $newest.LastWriteTime
    if ($age.TotalHours -gt $BackupMaxAgeHours) {
        return ("the newest backup ({0}) is {1:N0} hours old" -f $newest.Name, $age.TotalHours)
    }
    return $null
}

# ---------- state and alerts ----------

$state = [pscustomobject]@{ ApiFailures = 0; ApiDown = $false; ApiProblem = ''; BackupLate = $false }
if (Test-Path $StateFile) {
    try { $state = Get-Content -Path $StateFile -Raw | ConvertFrom-Json } catch { }
}

$settings = $null
try { $settings = Read-ErfWebSettings -InstallPath $InstallPath } catch { Write-MaintenanceLog -LogFile $logFile -Message "Cannot read appsettings.json: alerts cannot be emailed." }

function Alert([string] $subject, [string] $body) {
    Write-MaintenanceLog -LogFile $logFile -Message $subject
    if ($settings -and $AlertTo.Count -gt 0) {
        Send-ErfWebAlert -Settings $settings -To $AlertTo -Subject $subject -Body $body -LogFile $logFile
    }
}

$apiProblem = Test-Api
if ($apiProblem) {
    $state.ApiFailures = [int] $state.ApiFailures + 1
    if (-not $state.ApiDown -and $state.ApiFailures -ge $FailuresBeforeAlert) {
        $state.ApiDown = $true
        $state.ApiProblem = $apiProblem
        Alert 'ERFWeb is DOWN' "ERFWeb is not working: $apiProblem.`r`n`r`nChecked: $HealthUrl ($($state.ApiFailures) checks in a row).`r`nSee the Troubleshooting table in the ERFWeb README and the newest log in $InstallPath\logs."
    }
}
else {
    if ($state.ApiDown) {
        Alert 'ERFWeb is back up' "ERFWeb answers normally again at $HealthUrl.`r`n`r`nThe problem was: $($state.ApiProblem)."
    }
    $state.ApiFailures = 0
    $state.ApiDown = $false
    $state.ApiProblem = ''
}

$backupProblem = Test-Backup
if ($backupProblem -and -not $state.BackupLate) {
    $state.BackupLate = $true
    Alert 'Database backup is MISSING' "The nightly ERFWeb database backup did not run: $backupProblem.`r`n`r`nSee $BackupDir\backup-log.txt and the 'ERFWeb Database Backup' task in Task Scheduler."
}
elseif (-not $backupProblem -and $state.BackupLate) {
    $state.BackupLate = $false
    Alert 'Database backup is running again' "A recent ERFWeb database backup is in $BackupDir again."
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $StateFile) | Out-Null
$state | ConvertTo-Json | Set-Content -Path $StateFile -Encoding UTF8
