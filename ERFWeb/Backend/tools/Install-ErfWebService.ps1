<#
.SYNOPSIS
    Installs (or updates, or removes) the ERFWeb API as a Windows Service on the server.

.DESCRIPTION
    Run once on the server from an elevated PowerShell (or double-click Install-ErfWebService.cmd in a release
    folder, which also passes -ReleaseZip so the API files are extracted first). The app folder must hold the
    server's own appsettings.json. The service:
      * runs ERFWeb.Api.exe from InstallPath, starts with Windows, and restarts itself after a crash;
      * runs as the virtual account "NT SERVICE\<ServiceName>" (no password, no admin rights), which gets
        read access to InstallPath and write access to its logs folder only;
      * logs to <InstallPath>\logs\erfweb-yyyyMMdd.log and warnings to the Windows Application event log.
    Stop any console copy of the API (Ctrl+C) first: both cannot listen on the same port.

    Running it again on an installed service stops it, refreshes the settings above and starts it again,
    so it is safe after a deploy. -Uninstall stops and removes the service (files are left alone).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-ErfWebService.ps1
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-ErfWebService.ps1 -ReleaseZip .\ERFWeb-Api.zip
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-ErfWebService.ps1 -InstallPath D:\erf -Uninstall
#>
[CmdletBinding()]
param(
    [string] $InstallPath = 'C:\inetpub\wwwroot\erf_web-app',
    [string] $ServiceName = 'ERFWeb.Api',
    # A release's ERFWeb-Api.zip: extracted into InstallPath before the service is set up.
    [string] $ReleaseZip,
    [switch] $Uninstall
)

$ErrorActionPreference = 'Stop'

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script from an elevated PowerShell (Run as administrator).'
}

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($Uninstall) {
    if (-not $existing) { Write-Host "Service '$ServiceName' is not installed."; return }
    if ($existing.Status -ne 'Stopped') { Stop-Service -Name $ServiceName -Force; $existing.WaitForStatus('Stopped', '00:00:30') }
    & sc.exe delete $ServiceName | Out-Null
    Write-Host "Service '$ServiceName' removed. Files in $InstallPath were left in place."
    return
}

if ($ReleaseZip) {
    if (-not (Test-Path $ReleaseZip)) { throw "Release zip not found: $ReleaseZip" }
    # Files of a running API are locked: stop it (or the old console copy must already be closed) first.
    if ($existing -and $existing.Status -ne 'Stopped') { Stop-Service -Name $ServiceName -Force; $existing.WaitForStatus('Stopped', '00:00:30') }
    New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null
    Expand-Archive -Path $ReleaseZip -DestinationPath $InstallPath -Force
    Write-Host "Extracted $ReleaseZip into $InstallPath."
}

$exe = Join-Path $InstallPath 'ERFWeb.Api.exe'
$settings = Join-Path $InstallPath 'appsettings.json'
if (-not (Test-Path $exe)) { throw "ERFWeb.Api.exe not found in $InstallPath. Run Install-ErfWebService.cmd from a release folder, or pass -ReleaseZip." }
if (-not (Test-Path $settings)) { throw "appsettings.json not found in $InstallPath. Create it from appsettings.Example.json (in that folder) with the server's values, then run this again." }

# Event log source for warnings and errors (creating it needs admin, so it is done here, not by the service).
if (-not [Diagnostics.EventLog]::SourceExists($ServiceName)) {
    New-EventLog -LogName Application -Source $ServiceName
}

if ($existing) {
    if ($existing.Status -ne 'Stopped') { Stop-Service -Name $ServiceName -Force; $existing.WaitForStatus('Stopped', '00:00:30') }
    & sc.exe config $ServiceName binPath= "`"$exe`"" start= auto | Out-Null
} else {
    New-Service -Name $ServiceName -BinaryPathName "`"$exe`"" -DisplayName 'ERFWeb API' -StartupType Automatic `
        -Description 'ERFWeb expense report API (ASP.NET Core). Behind nginx at https://sr.mdmpi.com.ph/erf.' | Out-Null
}

$account = "NT SERVICE\$ServiceName"
& sc.exe config $ServiceName obj= $account | Out-Null

# Read and run the application; write only to logs\.
$logs = Join-Path $InstallPath 'logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
& icacls.exe $InstallPath /grant "${account}:(OI)(CI)RX" /T /Q | Out-Null
& icacls.exe $logs /grant "${account}:(OI)(CI)M" /T /Q | Out-Null

# Restart after 5 s, 5 s, then 30 s; the failure count resets after a day without failures.
& sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/30000 | Out-Null
& sc.exe failureflag $ServiceName 1 | Out-Null

Start-Service -Name $ServiceName
(Get-Service -Name $ServiceName).WaitForStatus('Running', '00:00:30')
Write-Host "Service '$ServiceName' is running as $account."

# Smoke test against the first Kestrel endpoint in appsettings.json.
$config = Get-Content -Raw -Path $settings | ConvertFrom-Json
$endpoint = $null
if ($config.Kestrel -and $config.Kestrel.Endpoints) {
    $endpoint = ($config.Kestrel.Endpoints.PSObject.Properties | Select-Object -First 1).Value.Url
}
if (-not $endpoint) { Write-Host 'No Kestrel endpoint in appsettings.json; skipped the health check.'; return }

$healthUrl = $endpoint.TrimEnd('/') + '/api/health'
Start-Sleep -Seconds 3
try {
    $health = Invoke-RestMethod -Uri $healthUrl -TimeoutSec 15
    Write-Host "Health: $($health.status), version $($health.version), database $($health.database)  ($healthUrl)"
} catch {
    Write-Warning "Health check at $healthUrl failed: $($_.Exception.Message). See $logs and the Application event log."
}
