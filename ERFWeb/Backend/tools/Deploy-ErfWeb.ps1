<#
.SYNOPSIS
    Installs the ERFWeb API release in this folder on the server, and rolls back if it does not come up healthy.

.DESCRIPTION
    Double-click Deploy-ErfWeb.cmd in the release folder that Publish-ErfWeb.cmd made. In order it:
      1. checks ERFWeb-Api.zip is here, the ERFWeb.Api service is installed and the app folder has its own
         appsettings.json;
      2. stops the service and backs up the app folder (logs excluded) to <app>-backups\<time>, keeping 3;
      3. extracts the release over the app folder (appsettings.json is never in the zip, so it is kept);
      4. starts the service and waits up to 60 s for /api/health to report this release and database "ok";
      5. if it does not, restores the backup, starts the old version again and says so.
    The first install is done once with Install-ErfWebService.ps1.
#>
[CmdletBinding()]
param(
    [string] $InstallPath = 'C:\inetpub\wwwroot\erf_web-app',
    [string] $ServiceName = 'ERFWeb.Api',
    [int] $KeepBackups = 3
)

$ErrorActionPreference = 'Stop'

function Write-Step([string] $text) { Write-Host ''; Write-Host "== $text" -ForegroundColor Cyan }

$zip = Join-Path $PSScriptRoot 'ERFWeb-Api.zip'
$expected = if (Test-Path (Join-Path $PSScriptRoot 'release.txt')) { (Get-Content (Join-Path $PSScriptRoot 'release.txt') -Raw).Trim() } else { $null }
$settings = Join-Path $InstallPath 'appsettings.json'

Write-Step "Deploying ERFWeb API $expected to $InstallPath"
if (-not (Test-Path $zip)) { throw "ERFWeb-Api.zip not found next to this script ($PSScriptRoot)." }
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) { throw "Service '$ServiceName' is not installed. Run Install-ErfWebService.ps1 once first." }
if (-not (Test-Path $settings)) { throw "No appsettings.json in $InstallPath. The server must keep its own (see appsettings.Example.json)." }

# Health URL from the first Kestrel endpoint in the server's appsettings.json.
$config = Get-Content -Raw -Path $settings | ConvertFrom-Json
$endpoint = ($config.Kestrel.Endpoints.PSObject.Properties | Select-Object -First 1).Value.Url
if (-not $endpoint) { throw 'No Kestrel endpoint in appsettings.json, so the health check cannot run.' }
$healthUrl = $endpoint.TrimEnd('/') + '/api/health'

function Wait-Healthy([string] $version) {
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 2
        try {
            $health = Invoke-RestMethod -Uri $healthUrl -TimeoutSec 10
            if ($health.database -eq 'ok' -and (-not $version -or $health.version -eq $version)) { return $health }
        } catch {
            # Not up yet, or 503 while the database check fails: keep waiting.
            Write-Verbose $_.Exception.Message
        }
    }
    return $null
}

function Stop-Api {
    $svc = Get-Service -Name $ServiceName
    if ($svc.Status -ne 'Stopped') { Stop-Service -Name $ServiceName -Force; $svc.WaitForStatus('Stopped', '00:00:30') }
}

Write-Step 'Stopping the service'
Stop-Api

Write-Step 'Backing up the current version'
$backupRoot = "$InstallPath-backups"
$backup = Join-Path $backupRoot (Get-Date -Format 'yyyyMMdd-HHmmss')
New-Item -ItemType Directory -Force -Path $backup | Out-Null
Get-ChildItem -Path $InstallPath -Force | Where-Object { $_.Name -ne 'logs' } |
    Copy-Item -Destination $backup -Recurse -Force
Get-ChildItem -Path $backupRoot -Directory | Sort-Object Name -Descending | Select-Object -Skip $KeepBackups |
    Remove-Item -Recurse -Force
Write-Host "Backup: $backup"

Write-Step 'Installing the new version'
Expand-Archive -Path $zip -DestinationPath $InstallPath -Force

Write-Step 'Starting and checking health'
Start-Service -Name $ServiceName
$health = Wait-Healthy $expected
if ($health) {
    Write-Host ''
    Write-Host "Deployed: version $($health.version), database $($health.database)." -ForegroundColor Green
    return
}

Write-Host ''
Write-Host "The new version did not report healthy at $healthUrl. Rolling back..." -ForegroundColor Yellow
Stop-Api
Get-ChildItem -Path $backup -Force | Copy-Item -Destination $InstallPath -Recurse -Force
Start-Service -Name $ServiceName
$restored = Wait-Healthy $null
if ($restored) {
    Write-Host "Rolled back to version $($restored.version). The new release was NOT deployed; see $InstallPath\logs and the Application event log." -ForegroundColor Yellow
} else {
    Write-Host "Rollback started the old files but health still fails. Check $InstallPath\logs and the Application event log now." -ForegroundColor Red
}
exit 1
