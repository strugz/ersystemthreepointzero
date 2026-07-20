[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Install', 'Update', 'Uninstall', 'Start', 'Stop', 'Status')]
    [string]$Action,

    [string]$ExecutablePath,

    [string]$SettingsPath
)

$ErrorActionPreference = 'Stop'
$serviceName = 'ERSystemApprovalReminders'
$displayName = 'ER System Approval Reminders'
$serviceAccount = "NT SERVICE\$serviceName"

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Run this script from an elevated PowerShell session.'
    }
}

function Invoke-ServiceControl {
    param([string[]]$Arguments)

    & sc.exe @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "sc.exe failed with exit code $LASTEXITCODE."
    }
}

function Get-ServiceCommandLine {
    if ([string]::IsNullOrWhiteSpace($ExecutablePath) -or [string]::IsNullOrWhiteSpace($SettingsPath)) {
        throw 'ExecutablePath and SettingsPath are required for Install and Update.'
    }

    $resolvedExecutable = [System.IO.Path]::GetFullPath($ExecutablePath)
    $resolvedSettings = [System.IO.Path]::GetFullPath($SettingsPath)
    if (-not (Test-Path -LiteralPath $resolvedExecutable -PathType Leaf)) {
        throw "The worker executable does not exist: $resolvedExecutable"
    }
    if (-not (Test-Path -LiteralPath $resolvedSettings -PathType Leaf)) {
        throw "The protected settings file does not exist: $resolvedSettings"
    }

    return [pscustomobject]@{
        CommandLine = ('"{0}" --settings "{1}"' -f $resolvedExecutable, $resolvedSettings)
        ExecutableDirectory = Split-Path -Parent $resolvedExecutable
        SettingsFile = $resolvedSettings
    }
}

function Grant-ServiceFileAccess {
    param($CommandInfo)

    & icacls.exe $CommandInfo.ExecutableDirectory /grant "${serviceAccount}:(OI)(CI)RX" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to grant the service account read/execute access to the publish directory.'
    }

    & icacls.exe $CommandInfo.SettingsFile /grant "${serviceAccount}:R" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to grant the service account read access to the protected settings file.'
    }
}

Assert-Administrator
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

switch ($Action) {
    'Install' {
        if ($null -ne $existingService) {
            throw "Service $serviceName already exists. Use Update instead."
        }

        $commandInfo = Get-ServiceCommandLine
        Invoke-ServiceControl @('create', $serviceName, 'binPath=', $commandInfo.CommandLine, 'start=', 'delayed-auto', 'obj=', $serviceAccount, 'DisplayName=', $displayName)
        Invoke-ServiceControl @('description', $serviceName, 'Sends due ER approval email and SMS gateway reminders.')
        Invoke-ServiceControl @('failure', $serviceName, 'reset=', '86400', 'actions=', 'restart/60000/restart/60000/none/0')
        Grant-ServiceFileAccess $commandInfo
        if (-not [System.Diagnostics.EventLog]::SourceExists($displayName)) {
            New-EventLog -LogName Application -Source $displayName
        }
        Start-Service -Name $serviceName
        Write-Host "Installed and started $displayName under $serviceAccount."
    }
    'Update' {
        if ($null -eq $existingService) {
            throw "Service $serviceName is not installed."
        }

        $commandInfo = Get-ServiceCommandLine
        if ($existingService.Status -ne 'Stopped') {
            Stop-Service -Name $serviceName
            (Get-Service -Name $serviceName).WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
        }
        Invoke-ServiceControl @('config', $serviceName, 'binPath=', $commandInfo.CommandLine, 'start=', 'delayed-auto', 'obj=', $serviceAccount)
        Grant-ServiceFileAccess $commandInfo
        Start-Service -Name $serviceName
        Write-Host "Updated and restarted $displayName."
    }
    'Uninstall' {
        if ($null -eq $existingService) {
            Write-Host "Service $serviceName is not installed."
            break
        }

        if ($existingService.Status -ne 'Stopped') {
            Stop-Service -Name $serviceName
            (Get-Service -Name $serviceName).WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
        }
        Invoke-ServiceControl @('delete', $serviceName)
        Write-Host 'Removed the Windows Service registration. Published files, protected settings, database records, and reminder email addresses were preserved.'
    }
    'Start' {
        Start-Service -Name $serviceName
        Get-Service -Name $serviceName
    }
    'Stop' {
        Stop-Service -Name $serviceName
        Get-Service -Name $serviceName
    }
    'Status' {
        Get-Service -Name $serviceName
    }
}
